using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.SunHotels;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

/// <summary>
/// Otel rezervasyon akışı (Prebook → Payment → BookV3)
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/bookings/hotels")]
[Produces("application/json")]
public class HotelBookingsController : ControllerBase
{
    private readonly ISunHotelsService _sunHotelsService;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IEmailService _emailService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly ILogger<HotelBookingsController> _logger;

    public HotelBookingsController(
        ISunHotelsService sunHotelsService,
        IStripePaymentService stripePaymentService,
        IEmailService emailService,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMediator mediator,
        ILogger<HotelBookingsController> logger)
    {
        _sunHotelsService = sunHotelsService;
        _stripePaymentService = stripePaymentService;
        _emailService = emailService;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Ön-rezervasyon (Fiyat ve vergi bilgisini kontrol et)
    /// Frontend: Oda seçiminden sonra çağrılıyor, Stripe payment intent oluşturuluyor
    /// Misafir kullanıcılar da rezervasyon yapabilir
    /// </summary>
    [AllowAnonymous]
    [HttpPost("prebook")]
    [ProducesResponseType(typeof(HotelPreBookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PreBook([FromBody] HotelPreBookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Misafir bilgileri zorunlu (kullanıcı login olmasa da form doldurur)
            if (string.IsNullOrWhiteSpace(request.GuestEmail) || string.IsNullOrWhiteSpace(request.GuestName))
            {
                return BadRequest(new { message = "Misafir adı ve email zorunludur." });
            }

            _logger.LogInformation("PreBook request for Hotel {HotelId}, Guest: {GuestEmail}", request.HotelId, request.GuestEmail);

            // SunHotels PreBook V3 çağrısı
            var preBookRequest = new SunHotelsPreBookRequestV3
            {
                HotelId = request.HotelId,
                RoomId = request.RoomId,
                RoomTypeId = request.RoomTypeId,
                MealId = request.MealId,
                CheckIn = request.CheckInDate,
                CheckOut = request.CheckOutDate,
                Rooms = request.Rooms,
                Adults = request.Adults,
                Children = request.Children,
                ChildrenAges = request.ChildrenAges ?? "",
                Infant = request.Infant ?? 0,
                Currency = request.Currency ?? "EUR",
                Language = request.Language ?? "en",
                B2C = request.IsSuperDeal, // SuperDeal otelleri için B2C = true
                SearchPrice = request.SearchPrice,
                CustomerCountry = request.CustomerCountry ?? "TR",
                BlockSuperDeal = false,
                ShowPriceBreakdown = true,
                PaymentMethodId = 0
            };

            var preBookResult = await _sunHotelsService.PreBookV3Async(preBookRequest, cancellationToken);

            _logger.LogInformation("PreBookV3 result - TotalPrice: {TotalPrice} {Currency}, PreBookCode: {PreBookCode}, HasError: {HasError}",
                preBookResult.TotalPrice, preBookResult.Currency, preBookResult.PreBookCode, preBookResult.HasError);

            // ⚠️ SunHotels hata kontrolü
            if (preBookResult.HasError)
            {
                _logger.LogError("SunHotels PreBookV3 error - Code: {ErrorCode}, Message: {Error}",
                    preBookResult.ErrorCode, preBookResult.Error);
                return BadRequest(new
                {
                    message = "Otel müsaitlik bilgisi alınamadı. Lütfen tekrar deneyin.",
                    technicalError = preBookResult.Error
                });
            }

            // ⚠️ TotalPrice validation - minimum check
            if (preBookResult.TotalPrice < 0.50m)
            {
                _logger.LogError("PreBookV3 returned invalid TotalPrice: {TotalPrice}. SunHotels may have returned an error.", preBookResult.TotalPrice);
                return BadRequest(new { message = "Fiyat bilgisi alınamadı. Lütfen tekrar deneyin." });
            }

            // ⚠️ PreBookCode expiry kontrolü
            if (preBookResult.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("PreBookCode already expired: {ExpiresAt}", preBookResult.ExpiresAt);
                return BadRequest(new { message = "Rezervasyon zaman aşımına uğradı. Lütfen tekrar deneyin." });
            }

            // Fiyat değişimi kontrolü
            if (preBookResult.PriceChanged)
            {
                _logger.LogWarning("Price changed for HotelId: {HotelId}, Original: {Original}, New: {New}",
                    request.HotelId, preBookResult.OriginalPrice, preBookResult.TotalPrice);
            }

            // Temp booking oluştur (Pending status)
            // Kullanıcı login ise UserId kaydet, değilse null
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId, // Nullable - misafir için null olabilir
                Type = BookingType.Hotel,
                Status = BookingStatus.Pending, // Stripe ödeme öncesi
                TotalPrice = preBookResult.TotalPrice,
                Commission = preBookResult.TotalPrice * 0.10m,
                Currency = preBookResult.Currency,
                CouponDiscount = 0,
                VerificationToken = Guid.NewGuid().ToString("N") // Guest payment için
            };

            // İptal politikası bilgilerini hesapla
            var isRefundable = true;
            decimal cancellationPercentage = 0;
            DateTime? freeCancellationDeadline = null;
            string? cancellationPolicyText = null;

            if (preBookResult.CancellationPolicies != null && preBookResult.CancellationPolicies.Any())
            {
                // En yüksek yüzdeyi bul (genellikle deadline'a göre sıralı gelir)
                var highestPolicy = preBookResult.CancellationPolicies.OrderByDescending(p => p.Percentage).FirstOrDefault();
                if (highestPolicy != null)
                {
                    cancellationPercentage = highestPolicy.Percentage;
                    freeCancellationDeadline = preBookResult.EarliestNonFreeCancellationDateCET.HasValue
                        ? DateTime.SpecifyKind(preBookResult.EarliestNonFreeCancellationDateCET.Value, DateTimeKind.Utc)
                        : highestPolicy.FromDate != DateTime.MinValue
                            ? DateTime.SpecifyKind(highestPolicy.FromDate, DateTimeKind.Utc)
                            : null;

                    // %100 iptal ücreti varsa non-refundable
                    isRefundable = cancellationPercentage < 100;

                    // Policy text oluştur
                    if (cancellationPercentage >= 100)
                    {
                        cancellationPolicyText = "Non-refundable. No cancellation or changes allowed.";
                    }
                    else if (freeCancellationDeadline.HasValue)
                    {
                        cancellationPolicyText = $"Free cancellation until {freeCancellationDeadline.Value:dd MMM yyyy}. After this date, {cancellationPercentage}% cancellation fee applies.";
                    }
                }
            }

            // Maksimum iade edilebilir tutarı hesapla
            decimal? maxRefundableAmount = isRefundable
                ? preBookResult.TotalPrice * (1 - (cancellationPercentage / 100))
                : 0;

            var hotelBooking = new HotelBooking
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                HotelId = null, // SunHotels otelleri için null - ihtiyaç duyulduğunda ExternalHotelId ile eşleştirilebilir
                ExternalHotelId = request.HotelId, // SunHotels HotelId (int)
                RoomId = request.RoomId,
                RoomTypeId = request.RoomTypeId,
                CheckIn = DateTime.SpecifyKind(request.CheckInDate, DateTimeKind.Utc),
                CheckOut = DateTime.SpecifyKind(request.CheckOutDate, DateTimeKind.Utc),
                Adults = request.Adults,
                Children = request.Children,
                GuestName = request.GuestName,
                GuestEmail = request.GuestEmail,
                SpecialRequests = request.SpecialRequests,
                MealId = request.MealId,
                PreBookCode = preBookResult.PreBookCode, // SunHotels PreBook code'u kaydet
                IsSuperDeal = request.IsSuperDeal, // Son dakika fırsatı bilgisini kaydet

                // Cancellation & Refund bilgileri
                IsRefundable = isRefundable,
                FreeCancellationDeadline = freeCancellationDeadline,
                CancellationPercentage = cancellationPercentage,
                MaxRefundableAmount = maxRefundableAmount,
                CancellationPolicyText = cancellationPolicyText,
                CancellationPolicies = preBookResult.CancellationPolicies != null && preBookResult.CancellationPolicies.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(preBookResult.CancellationPolicies)
                    : null
            };

            booking.HotelBooking = hotelBooking;

            await _bookingRepository.AddAsync(booking, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Stripe PaymentIntent oluştur
            var metadata = new Dictionary<string, string>
            {
                { "bookingType", "Hotel" },
                { "hotelId", request.HotelId.ToString() },
                { "prebook_code", preBookResult.PreBookCode },
                { "checkInDate", request.CheckInDate.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOutDate.ToString("yyyy-MM-dd") }
            };

            var clientSecret = await _stripePaymentService.CreatePaymentIntentAsync(
                booking.TotalPrice,
                booking.Currency,
                booking.Id,
                metadata,
                cancellationToken);

            return Ok(new HotelPreBookResponse(
                Success: true,
                BookingId: booking.Id,
                PreBookCode: preBookResult.PreBookCode,
                TotalPrice: booking.TotalPrice,
                TaxAmount: preBookResult.TaxAmount ?? 0,
                Currency: booking.Currency,
                PriceChanged: preBookResult.PriceChanged,
                OriginalPrice: preBookResult.OriginalPrice,
                ExpiresAt: preBookResult.ExpiresAt,
                HotelConfirmationNumber: null,
                VerificationToken: booking.VerificationToken,
                ClientSecret: clientSecret,
                Fees: preBookResult.PriceBreakdown?.Select(p => new FeeDto(p.Date.ToString("dd.MM.yyyy"), p.Price, p.Currency)).ToList(),
                Message: "Rezervasyon bilgileriniz alındı. Ödeme işlemine geçebilirsiniz."
            ));
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "SunHotels PreBook failed");
            var friendlyMessage = SunHotelsErrorHelper.GetFriendlyErrorFromException(ex);
            return StatusCode(502, new { message = friendlyMessage, technicalError = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PreBook endpoint error");
            var friendlyMessage = SunHotelsErrorHelper.GetFriendlyErrorFromException(ex);
            return StatusCode(500, new { message = friendlyMessage });
        }
    }

    /// <summary>
    /// Ödemeden sonra nihai rezervasyon (BookV3)
    /// Webhook'tan veya PreBookCode ile çağrılıyor
    /// </summary>
    [HttpPost("confirm")]
    [AllowAnonymous] // Internal endpoint (webhook çağırıyor)
    [ProducesResponseType(typeof(HotelBookConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmBooking(
        [FromBody] HotelBookConfirmRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Booking? booking = null;

            // Öncelik 1: PreBookCode ile bul
            if (!string.IsNullOrEmpty(request.PreBookCode))
            {
                _logger.LogInformation("ConfirmBooking for PreBookCode: {PreBookCode}", request.PreBookCode);
                booking = await _bookingRepository.GetByPreBookCodeAsync(request.PreBookCode, cancellationToken);
            }
            // Öncelik 2: BookingId ile bul
            else if (request.BookingId.HasValue)
            {
                _logger.LogInformation("ConfirmBooking for BookingId: {BookingId}", request.BookingId);
                booking = await _bookingRepository.GetByIdWithDetailsAsync(request.BookingId.Value, cancellationToken);
            }

            if (booking == null)
            {
                return NotFound(new { success = false, message = "Rezervasyon bulunamadı." });
            }

            if (booking.HotelBooking == null)
            {
                return BadRequest(new { success = false, message = "Otel rezervasyonu bilgisi eksik." });
            }

            // Kendi durumlarının kontrol
            if (booking.Status != BookingStatus.Pending)
            {
                // Zaten confirmed ise, mevcut bilgileri döndür
                if (booking.Status == BookingStatus.Confirmed)
                {
                    return Ok(new HotelBookConfirmResponse(
                        Success: true,
                        BookingId: $"BK-{booking.CreatedAt:yyyy}-{booking.Id.ToString()[..6].ToUpper()}",
                        SunhotelsBookingCode: booking.HotelBooking.ConfirmationCode ?? "",
                        HotelConfirmationNumber: booking.HotelBooking.ConfirmationCode,
                        Status: "confirmed",
                        Voucher: new VoucherDto(
                            VoucherNumber: $"V-{booking.Id.ToString()[..6].ToUpper()}",
                            DownloadUrl: $"/api/v1/bookings/{booking.Id}/voucher"
                        ),
                        CheckInDate: booking.HotelBooking.CheckIn,
                        CheckOutDate: booking.HotelBooking.CheckOut,
                        TotalPrice: booking.TotalPrice,
                        Currency: booking.Currency,
                        Message: "Bu rezervasyon zaten onaylanmış."
                    ));
                }
                return BadRequest(new { success = false, message = "Bu rezervasyon zaten işlendi." });
            }

            // Stripe Test Mode bilgisi (loglama için)
            var isTestMode = await _stripePaymentService.IsTestModeAsync(cancellationToken);
            _logger.LogInformation("ConfirmBooking - Stripe Mode: {Mode}, BookingId: {BookingId}",
                isTestMode ? "TEST" : "LIVE", booking.Id);

            string bookingNumber;
            bool isTestBooking = isTestMode; // Test mode bilgisi korunuyor (response için)

            // Her zaman gerçek SunHotels BookV3 çağrısı yap (test mode dahil)
            {
                var adultGuestFirstName = booking.HotelBooking.GuestName?.Split(' ').FirstOrDefault() ?? "Guest";
                var adultGuestLastName = booking.HotelBooking.GuestName?.Split(' ').LastOrDefault() ?? "User";

                var bookV3Request = new SunHotelsBookRequestV3
                {
                    PreBookCode = booking.HotelBooking.PreBookCode ?? throw new InvalidOperationException("PreBookCode is required"),
                    RoomId = booking.HotelBooking.RoomId,
                    MealId = booking.HotelBooking.MealId,
                    CheckIn = booking.HotelBooking.CheckIn,
                    CheckOut = booking.HotelBooking.CheckOut,
                    Rooms = booking.HotelBooking.Adults + booking.HotelBooking.Children > 0 ? 1 : 0,
                    Adults = booking.HotelBooking.Adults,
                    Children = booking.HotelBooking.Children,
                    Infant = 0,
                    Currency = booking.Currency,
                    Language = booking.User?.Locale ?? "tr",
                    Email = booking.HotelBooking.GuestEmail ?? throw new InvalidOperationException("Guest email is required"),
                    YourRef = $"FS-{booking.Id}",
                    CustomerCountry = request.GuestCountry ?? "TR",
                    SpecialRequest = booking.HotelBooking.SpecialRequests ?? "",
                    PaymentMethodId = 0,
                    B2C = booking.HotelBooking.IsSuperDeal,
                    AdultGuests = GenerateAdultGuestList(booking.HotelBooking.Adults, adultGuestFirstName, adultGuestLastName)
                };

                var bookResult = await _sunHotelsService.BookV3Async(bookV3Request, cancellationToken);
                bookingNumber = bookResult.BookingNumber ?? "UNKNOWN";

                _logger.LogInformation("SunHotels BookV3 completed - BookingNumber: {BookingNumber}, IsTestMode: {IsTestMode}",
                    bookingNumber, isTestMode);
            }

            // Booking status güncelle
            booking.Status = BookingStatus.Confirmed;
            booking.HotelBooking.ConfirmationCode = bookingNumber;

            if (booking.Payment != null)
            {
                booking.Payment.Status = PaymentStatus.Completed;
                booking.Payment.PaidAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Confirmation email gönder (test modunda da gönderilir - test amaçlı)
            if (!isTestBooking)
            {
                // Sadece gerçek booking için email gönder
                // Test modunda mock bookResult olmadığı için bu metodu çağıramıyoruz
            }

            // README formatına uygun response
            var bookingIdFormatted = $"BK-{booking.CreatedAt:yyyy}-{booking.Id.ToString()[..6].ToUpper()}";
            var statusMessage = isTestBooking
                ? "TEST MODE - Rezervasyon simüle edildi. Gerçek SunHotels booking yapılmadı."
                : "Rezervasyonunuz başarıyla tamamlandı. Lütfen onay e-postasını kontrol edin.";

            return Ok(new HotelBookConfirmResponse(
                Success: true,
                BookingId: bookingIdFormatted,
                SunhotelsBookingCode: bookingNumber,
                HotelConfirmationNumber: bookingNumber,
                Status: isTestBooking ? "test_confirmed" : "confirmed",
                Voucher: new VoucherDto(
                    VoucherNumber: $"V-{booking.Id.ToString()[..6].ToUpper()}",
                    DownloadUrl: $"/api/v1/bookings/{booking.Id}/voucher"
                ),
                CheckInDate: booking.HotelBooking.CheckIn,
                CheckOutDate: booking.HotelBooking.CheckOut,
                TotalPrice: booking.TotalPrice,
                Currency: booking.Currency,
                Message: statusMessage
            ));
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "SunHotels BookV3 failed for request: {Request}",
                request.PreBookCode ?? request.BookingId?.ToString() ?? "unknown");

            // Update booking status to failed
            if (request.BookingId.HasValue)
            {
                var booking = await _bookingRepository.GetByIdAsync(request.BookingId.Value);
                if (booking != null)
                {
                    booking.Status = BookingStatus.Failed;
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var friendlyMessage = SunHotelsErrorHelper.GetFriendlyErrorFromException(ex);
            return StatusCode(502, new { success = false, message = friendlyMessage, technicalError = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmBooking endpoint error");
            var friendlyMessage = SunHotelsErrorHelper.GetFriendlyErrorFromException(ex);
            return StatusCode(500, new { success = false, message = friendlyMessage });
        }
    }

    private async Task SendHotelBookingConfirmationEmailAsync(Booking booking, SunHotelsBookResultV3 bookResult, CancellationToken cancellationToken)
    {
        try
        {
            var variables = new Dictionary<string, string>
            {
                { "userName", booking.HotelBooking?.GuestName ?? booking.User?.Name ?? "Müşteri" },
                { "bookingId", booking.Id.ToString() },
                { "hotelName", booking.HotelBooking?.Hotel?.Name ?? "Hotel" },
                { "checkInDate", booking.HotelBooking?.CheckIn.ToString("dd.MM.yyyy") ?? "" },
                { "checkOutDate", booking.HotelBooking?.CheckOut.ToString("dd.MM.yyyy") ?? "" },
                { "guestCount", $"{booking.HotelBooking?.Adults}" },
                { "roomType", booking.HotelBooking?.RoomTypeName ?? "Standard" },
                { "totalAmount", booking.TotalPrice.ToString("N2") },
                { "currency", booking.Currency },
                { "confirmationCode", bookResult.BookingNumber ?? "PENDING" },
                { "reservationId", bookResult.BookingNumber ?? "PENDING" }
            };

            var email = booking.HotelBooking?.GuestEmail ?? booking.User?.Email;
            if (!string.IsNullOrEmpty(email))
            {
                await _emailService.SendTemplateEmailAsync(
                    email,
                    "hotel_booking_confirmed",
                    variables,
                    booking.User?.Locale ?? "tr",
                    cancellationToken);

                _logger.LogInformation("Confirmation email sent for Booking: {BookingId}", booking.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email for Booking: {BookingId}", booking.Id);
        }
    }

    /// <summary>
    /// PreBook'taki Adults sayısına göre guest listesi oluşturur
    /// İlk guest gerçek bilgi, diğerleri dummy
    /// </summary>
    private static List<SunHotelsGuest> GenerateAdultGuestList(int adultsCount, string firstName, string lastName)
    {
        var guests = new List<SunHotelsGuest>();

        for (int i = 0; i < adultsCount && i < 9; i++)
        {
            if (i == 0)
            {
                // İlk guest gerçek bilgi
                guests.Add(new SunHotelsGuest
                {
                    FirstName = firstName,
                    LastName = lastName
                });
            }
            else
            {
                // Diğer guest'ler dummy bilgi
                guests.Add(new SunHotelsGuest
                {
                    FirstName = "Guest",
                    LastName = $"User{i + 1}"
                });
            }
        }

        return guests;
    }

    /// <summary>
    /// Stripe Checkout Session oluştur (PreBook + Checkout)
    /// Frontend: Kullanıcı rezervasyon başlattığında çağrılır
    /// Misafir kullanıcılar da rezervasyon yapabilir
    /// </summary>
    [AllowAnonymous]
    [HttpPost("checkout-session")]
    [ProducesResponseType(typeof(CheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] HotelCheckoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Misafir bilgileri zorunlu (kullanıcı login olmasa da form doldurur)
            if (string.IsNullOrWhiteSpace(request.GuestEmail) || string.IsNullOrWhiteSpace(request.GuestName))
            {
                return BadRequest(new { message = "Misafir adı ve email zorunludur." });
            }

            _logger.LogInformation("Creating checkout session for Hotel {HotelId}, Guest: {GuestEmail}", request.HotelId, request.GuestEmail);

            // 1. SunHotels PreBook V3 çağrısı
            var preBookRequest = new SunHotelsPreBookRequestV3
            {
                HotelId = request.HotelId,
                RoomId = request.RoomId,
                RoomTypeId = request.RoomTypeId,
                MealId = request.MealId,
                CheckIn = request.CheckInDate,
                CheckOut = request.CheckOutDate,
                Rooms = request.Rooms,
                Adults = request.Adults,
                Children = request.Children,
                ChildrenAges = request.ChildrenAges ?? "",
                Infant = request.Infant ?? 0,
                Currency = request.Currency ?? "EUR",
                Language = request.Language ?? "en",
                B2C = request.IsSuperDeal,
                SearchPrice = request.SearchPrice,
                CustomerCountry = request.CustomerCountry ?? "TR",
                BlockSuperDeal = false,
                ShowPriceBreakdown = true,
                PaymentMethodId = 0
            };

            var preBookResult = await _sunHotelsService.PreBookV3Async(preBookRequest, cancellationToken);

            // 2. Database'e Pending booking kaydı
            // Kullanıcı login ise UserId kaydet, değilse null (misafir rezervasyonu)
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId, // Nullable - misafir için null olabilir
                Type = BookingType.Hotel,
                Status = BookingStatus.Pending,
                TotalPrice = preBookResult.TotalPrice,
                Commission = preBookResult.TotalPrice * 0.10m,
                Currency = preBookResult.Currency,
                CouponDiscount = 0,
                VerificationToken = Guid.NewGuid().ToString("N")
            };

            // İptal politikası bilgilerini hesapla
            var isRefundable2 = true;
            decimal cancellationPercentage2 = 0;
            DateTime? freeCancellationDeadline2 = null;
            string? cancellationPolicyText2 = null;

            if (preBookResult.CancellationPolicies != null && preBookResult.CancellationPolicies.Any())
            {
                var highestPolicy2 = preBookResult.CancellationPolicies.OrderByDescending(p => p.Percentage).FirstOrDefault();
                if (highestPolicy2 != null)
                {
                    cancellationPercentage2 = highestPolicy2.Percentage;
                    freeCancellationDeadline2 = preBookResult.EarliestNonFreeCancellationDateCET.HasValue
                        ? DateTime.SpecifyKind(preBookResult.EarliestNonFreeCancellationDateCET.Value, DateTimeKind.Utc)
                        : highestPolicy2.FromDate != DateTime.MinValue
                            ? DateTime.SpecifyKind(highestPolicy2.FromDate, DateTimeKind.Utc)
                            : null;
                    isRefundable2 = cancellationPercentage2 < 100;

                    if (cancellationPercentage2 >= 100)
                    {
                        cancellationPolicyText2 = "Non-refundable. No cancellation or changes allowed.";
                    }
                    else if (freeCancellationDeadline2.HasValue)
                    {
                        cancellationPolicyText2 = $"Free cancellation until {freeCancellationDeadline2.Value:dd MMM yyyy}. After this date, {cancellationPercentage2}% cancellation fee applies.";
                    }
                }
            }

            decimal? maxRefundableAmount2 = isRefundable2
                ? preBookResult.TotalPrice * (1 - (cancellationPercentage2 / 100))
                : 0;

            var hotelBooking = new HotelBooking
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                HotelId = null, // SunHotels otelleri için null
                ExternalHotelId = request.HotelId, // SunHotels HotelId (int)
                RoomId = request.RoomId,
                RoomTypeId = request.RoomTypeId,
                CheckIn = DateTime.SpecifyKind(request.CheckInDate, DateTimeKind.Utc),
                CheckOut = DateTime.SpecifyKind(request.CheckOutDate, DateTimeKind.Utc),
                Adults = request.Adults,
                Children = request.Children,
                GuestName = request.GuestName,
                GuestEmail = request.GuestEmail,
                SpecialRequests = request.SpecialRequests,
                MealId = request.MealId,
                PreBookCode = preBookResult.PreBookCode,
                IsSuperDeal = request.IsSuperDeal,

                // Cancellation & Refund bilgileri
                IsRefundable = isRefundable2,
                FreeCancellationDeadline = freeCancellationDeadline2,
                CancellationPercentage = cancellationPercentage2,
                MaxRefundableAmount = maxRefundableAmount2,
                CancellationPolicyText = cancellationPolicyText2,
                CancellationPolicies = preBookResult.CancellationPolicies != null && preBookResult.CancellationPolicies.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(preBookResult.CancellationPolicies)
                    : null
            };

            booking.HotelBooking = hotelBooking;

            await _bookingRepository.AddAsync(booking, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. Stripe Checkout Session oluştur - README formatına uygun metadata
            var metadata = new Dictionary<string, string>
            {
                { "bookingId", booking.Id.ToString() },
                { "bookingType", "Hotel" },
                { "preBookCode", preBookResult.PreBookCode },
                { "hotelId", request.HotelId.ToString() },
                { "roomId", request.RoomId.ToString() },
                { "checkInDate", request.CheckInDate.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOutDate.ToString("yyyy-MM-dd") },
                { "guestName", request.GuestName },
                { "guestEmail", request.GuestEmail }
            };

            // Pass/Kupon bilgileri varsa ekle
            if (!string.IsNullOrEmpty(request.PassPurchaseType))
            {
                metadata.Add("passPurchaseType", request.PassPurchaseType);
            }

            var sessionId = await _stripePaymentService.CreateCheckoutSessionAsync(
                booking.TotalPrice,
                booking.Currency,
                booking.Id,
                request.SuccessUrl,
                request.CancelUrl,
                metadata,
                cancellationToken);

            // README formatına uygun response
            return Ok(new CheckoutSessionResponse(
                Success: true,
                SessionId: sessionId,
                Url: $"https://checkout.stripe.com/pay/{sessionId}", // Stripe Checkout URL
                BookingId: booking.Id,
                PreBookCode: preBookResult.PreBookCode,
                TotalPrice: booking.TotalPrice,
                Currency: booking.Currency,
                Message: "Checkout session oluşturuldu. Stripe'a yönlendirebilirsiniz."
            ));
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "SunHotels PreBook failed during checkout session creation");
            return StatusCode(502, new { message = "Otel sistemi ile bağlantı kurulamadı.", error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout session creation error");
            return StatusCode(500, new { message = "Checkout session oluşturulamadı.", error = ex.Message });
        }
    }

    /// <summary>
    /// Checkout session durumunu kontrol et (Success page için)
    /// Frontend success sayfasında session_id ile bu endpoint'i çağırır
    /// </summary>
    [AllowAnonymous]
    [HttpGet("checkout-session/{sessionId}/status")]
    [ProducesResponseType(typeof(CheckoutSessionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCheckoutSessionStatus(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking checkout session status for: {SessionId}", sessionId);

            // Stripe'dan checkout session bilgisini al
            var session = await _stripePaymentService.GetCheckoutSessionAsync(sessionId, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Checkout session not found: {SessionId}", sessionId);
                return NotFound(new { message = "Checkout session bulunamadı." });
            }

            // Session metadata'dan booking ID'yi al
            var bookingIdStr = session.Metadata?.GetValueOrDefault("bookingId");
            Booking? booking = null;

            if (!string.IsNullOrEmpty(bookingIdStr) && Guid.TryParse(bookingIdStr, out var bookingId))
            {
                booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, cancellationToken);
            }

            // Session durumuna göre response oluştur
            var isPaid = session.PaymentStatus == "paid";
            var isCompleted = session.Status == "complete";

            _logger.LogInformation("Checkout session status - SessionId: {SessionId}, Status: {Status}, PaymentStatus: {PaymentStatus}, BookingId: {BookingId}",
                sessionId, session.Status, session.PaymentStatus, bookingIdStr);

            // HotelName'i metadata'dan al (checkout session oluştururken eklenmişse)
            var hotelName = session.Metadata?.GetValueOrDefault("hotelName");

            return Ok(new CheckoutSessionStatusResponse(
                SessionId: sessionId,
                Status: session.Status ?? "unknown",
                PaymentStatus: session.PaymentStatus ?? "unknown",
                IsPaid: isPaid,
                IsCompleted: isCompleted && isPaid,
                BookingId: booking?.Id,
                BookingNumber: booking?.Id.ToString().Substring(0, 8).ToUpper(),
                HotelBookingCode: booking?.HotelBooking?.ConfirmationCode ?? booking?.HotelBooking?.ExternalBookingId,
                HotelName: hotelName,
                CheckIn: booking?.HotelBooking?.CheckIn,
                CheckOut: booking?.HotelBooking?.CheckOut,
                GuestName: booking?.HotelBooking?.GuestName,
                GuestEmail: booking?.HotelBooking?.GuestEmail,
                TotalPrice: booking?.TotalPrice ?? session.AmountTotal / 100m, // Stripe cent cinsinden döner
                Currency: booking?.Currency ?? session.Currency?.ToUpper() ?? "EUR",
                BookingStatus: booking?.Status.ToString(),
                PaymentCompletedAt: booking?.Payment?.PaidAt,
                Message: isPaid && isCompleted
                    ? "Ödemeniz başarıyla alındı. Rezervasyonunuz onaylandı."
                    : isPaid
                        ? "Ödemeniz alındı, rezervasyon işleniyor..."
                        : "Ödeme bekleniyor."
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking checkout session status: {SessionId}", sessionId);
            return StatusCode(500, new { message = "Checkout session durumu alınamadı.", error = ex.Message });
        }
    }


    #region DTOs

    /// <summary>
    /// Guest bilgisi (yetişkin)
    /// </summary>
    public record GuestDto(
        string FirstName,
        string LastName,
        string Type // "adult"
    );

    /// <summary>
    /// Çocuk guest bilgisi
    /// </summary>
    public record ChildDto(
        string FirstName,
        string LastName,
        int Age
    );

    /// <summary>
    /// İletişim bilgileri
    /// </summary>
    public record ContactInfoDto(
        string Email,
        string? Phone
    );

    /// <summary>
    /// PreBook isteği - README formatına uygun
    /// </summary>
    public record HotelPreBookRequest(
        int HotelId,
        int RoomId,
        int RoomTypeId,
        int MealId,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int Rooms,
        int Adults,
        int Children,
        string? ChildrenAges,
        int? Infant,
        string GuestName,
        string GuestEmail,
        string? GuestPhone,
        decimal SearchPrice,
        bool IsSuperDeal,
        // README formatı: guests array
        List<GuestDto>? Guests = null,
        // README formatı: children array  
        List<ChildDto>? ChildrenGuests = null,
        // README formatı: contactInfo object
        ContactInfoDto? ContactInfo = null,
        string? SpecialRequests = null,
        string? Currency = "EUR",
        string? Language = "en",
        string? CustomerCountry = "TR"
    );

    /// <summary>
    /// PreBook yanıtı - README formatına uygun
    /// </summary>
    public record HotelPreBookResponse(
        bool Success,
        Guid BookingId,
        string PreBookCode,
        decimal TotalPrice,
        decimal TaxAmount,
        string Currency,
        bool PriceChanged,
        decimal? OriginalPrice,
        DateTime ExpiresAt,
        string? HotelConfirmationNumber,
        // Legacy alanlar (uyumluluk için)
        string? VerificationToken = null,
        string? ClientSecret = null,
        List<FeeDto>? Fees = null,
        string? Message = null
    );

    /// <summary>
    /// Checkout Session isteği - README formatına uygun
    /// </summary>
    public record HotelCheckoutRequest(
        // PreBook'tan gelen kod (varsa mevcut PreBook kullanılır)
        string? PreBookCode,
        // Veya yeni PreBook için gerekli bilgiler
        int HotelId,
        int RoomId,
        int RoomTypeId,
        int MealId,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int Rooms,
        int Adults,
        int Children,
        string GuestName,
        string GuestEmail,
        string? Phone,
        string? SpecialRequests,
        decimal SearchPrice,
        bool IsSuperDeal,
        string SuccessUrl,
        string CancelUrl,
        // Otel ve oda bilgileri (Checkout sayfası için)
        string? HotelName = null,
        string? RoomType = null,
        // Pass/Kupon bilgileri
        string? PassPurchaseType = null, // "one_time", "subscription" vb.
        bool PassCodeValid = false,
        string? ChildrenAges = "",
        int? Infant = 0,
        string? Currency = "EUR",
        string? Language = "en",
        string? CustomerCountry = "TR"
    );

    /// <summary>
    /// Checkout Session yanıtı - README formatına uygun
    /// </summary>
    public record CheckoutSessionResponse(
        bool Success,
        string SessionId,
        string? Url, // Stripe Checkout URL
        Guid? BookingId = null,
        string? PreBookCode = null,
        decimal? TotalPrice = null,
        string? Currency = null,
        string? Message = null
    );

    public record FeeDto(
        string Name,
        decimal Amount,
        string Currency
    );

    /// <summary>
    /// Booking onaylama isteği - README formatına uygun
    /// PreBookCode veya BookingId ile çalışır
    /// </summary>
    public record HotelBookConfirmRequest(
        // Öncelik 1: PreBookCode ile bul
        string? PreBookCode = null,
        // Öncelik 2: BookingId ile bul
        Guid? BookingId = null,
        string? GuestCountry = "TR"
    );

    /// <summary>
    /// Voucher bilgisi
    /// </summary>
    public record VoucherDto(
        string VoucherNumber,
        string DownloadUrl
    );

    /// <summary>
    /// Booking onaylama yanıtı - README formatına uygun
    /// </summary>
    public record HotelBookConfirmResponse(
        bool Success,
        string BookingId, // String format (BK-2025-001234)
        string SunhotelsBookingCode,
        string? HotelConfirmationNumber,
        string Status,
        VoucherDto? Voucher,
        // Legacy alanlar (uyumluluk için)
        DateTime? CheckInDate = null,
        DateTime? CheckOutDate = null,
        decimal? TotalPrice = null,
        string? Currency = null,
        string? Message = null
    );

    /// <summary>
    /// Checkout Session Status yanıtı - Success page için
    /// </summary>
    public record CheckoutSessionStatusResponse(
        string SessionId,
        string Status,
        string PaymentStatus,
        bool IsPaid,
        bool IsCompleted,
        Guid? BookingId,
        string? BookingNumber,
        string? HotelBookingCode,
        string? HotelName,
        DateTime? CheckIn,
        DateTime? CheckOut,
        string? GuestName,
        string? GuestEmail,
        decimal? TotalPrice,
        string Currency,
        string? BookingStatus,
        DateTime? PaymentCompletedAt,
        string Message
    );

    #endregion

}
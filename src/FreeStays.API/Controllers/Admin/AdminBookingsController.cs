using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace FreeStays.API.Controllers.Admin;

/// <summary>
/// Admin Booking Management - Rezervasyon yönetimi
/// - Başarısız SunHotels rezervasyonlarını yeniden gönderme
/// - Stripe üzerinden iade işlemleri
/// </summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[ApiController]
[Route("api/v1/admin/bookings")]
[Produces("application/json")]
public class AdminBookingsController : ControllerBase
{
    private readonly FreeStaysDbContext _dbContext;
    private readonly ISunHotelsService _sunHotelsService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminBookingsController> _logger;

    public AdminBookingsController(
        FreeStaysDbContext dbContext,
        ISunHotelsService sunHotelsService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AdminBookingsController> logger)
    {
        _dbContext = dbContext;
        _sunHotelsService = sunHotelsService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    #region Booking List & Details

    /// <summary>
    /// Tüm rezervasyonları listeler (filtreleme ile)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BookingStatus? status = null,
        [FromQuery] BookingType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Bookings
            .Include(b => b.HotelBooking)
            .Include(b => b.Payment)
            .Include(b => b.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        if (type.HasValue)
            query = query.Where(b => b.Type == type.Value);

        if (fromDate.HasValue)
            query = query.Where(b => b.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(b => b.CreatedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b =>
                (b.HotelBooking != null && b.HotelBooking.GuestName != null && b.HotelBooking.GuestName.Contains(search)) ||
                (b.HotelBooking != null && b.HotelBooking.GuestEmail != null && b.HotelBooking.GuestEmail.Contains(search)) ||
                (b.HotelBooking != null && b.HotelBooking.ConfirmationCode != null && b.HotelBooking.ConfirmationCode.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingListDto
            {
                Id = b.Id,
                Type = b.Type.ToString(),
                Status = b.Status.ToString(),
                TotalPrice = b.TotalPrice,
                Currency = b.Currency,
                GuestName = b.HotelBooking != null ? b.HotelBooking.GuestName : null,
                GuestEmail = b.HotelBooking != null ? b.HotelBooking.GuestEmail : null,
                HotelName = b.HotelBooking != null ? b.HotelBooking.RoomTypeName : null,
                CheckIn = b.HotelBooking != null ? b.HotelBooking.CheckIn : null,
                CheckOut = b.HotelBooking != null ? b.HotelBooking.CheckOut : null,
                ConfirmationCode = b.HotelBooking != null ? b.HotelBooking.ConfirmationCode : null,
                PaymentStatus = b.Payment != null ? b.Payment.Status.ToString() : null,
                StripePaymentIntentId = b.Payment != null ? b.Payment.StripePaymentIntentId : null,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// Ödeme alınmış ama SunHotels rezervasyonu başarısız olan rezervasyonları listeler
    /// </summary>
    [HttpGet("failed-confirmations")]
    public async Task<IActionResult> GetFailedConfirmations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Bookings
            .Include(b => b.HotelBooking)
            .Include(b => b.Payment)
            .Include(b => b.User)
            .Where(b => b.Status == BookingStatus.ConfirmationFailed)
            .Where(b => b.Payment != null && b.Payment.Status == PaymentStatus.Completed);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new FailedConfirmationDto
            {
                BookingId = b.Id,
                GuestName = b.HotelBooking != null ? b.HotelBooking.GuestName : null,
                GuestEmail = b.HotelBooking != null ? b.HotelBooking.GuestEmail : null,
                GuestPhone = b.HotelBooking != null ? b.HotelBooking.GuestPhone : null,
                ExternalHotelId = b.HotelBooking != null ? b.HotelBooking.ExternalHotelId : 0,
                RoomId = b.HotelBooking != null ? b.HotelBooking.RoomId : 0,
                RoomTypeName = b.HotelBooking != null ? b.HotelBooking.RoomTypeName : null,
                MealId = b.HotelBooking != null ? b.HotelBooking.MealId : 0,
                CheckIn = b.HotelBooking != null ? b.HotelBooking.CheckIn : DateTime.MinValue,
                CheckOut = b.HotelBooking != null ? b.HotelBooking.CheckOut : DateTime.MinValue,
                Adults = b.HotelBooking != null ? b.HotelBooking.Adults : 0,
                Children = b.HotelBooking != null ? b.HotelBooking.Children : 0,
                PreBookCode = b.HotelBooking != null ? b.HotelBooking.PreBookCode : null,
                IsSuperDeal = b.HotelBooking != null && b.HotelBooking.IsSuperDeal,
                SpecialRequests = b.HotelBooking != null ? b.HotelBooking.SpecialRequests : null,
                TotalPrice = b.TotalPrice,
                Currency = b.Currency,
                PaymentAmount = b.Payment != null ? b.Payment.Amount : 0,
                PaymentStatus = b.Payment != null ? b.Payment.Status.ToString() : null,
                StripePaymentIntentId = b.Payment != null ? b.Payment.StripePaymentIntentId : null,
                PaidAt = b.Payment != null ? b.Payment.PaidAt : null,
                Notes = b.Notes,
                CreatedAt = b.CreatedAt,
                CanRetry = b.HotelBooking != null && !string.IsNullOrEmpty(b.HotelBooking.PreBookCode),
                CanRefund = b.Payment != null && !string.IsNullOrEmpty(b.Payment.StripePaymentIntentId),

                // Cancellation & Refund Info
                IsRefundable = b.HotelBooking != null && b.HotelBooking.IsRefundable,
                FreeCancellationDeadline = b.HotelBooking != null ? b.HotelBooking.FreeCancellationDeadline : null,
                CancellationPercentage = b.HotelBooking != null ? b.HotelBooking.CancellationPercentage : 0,
                MaxRefundableAmount = b.HotelBooking != null ? b.HotelBooking.MaxRefundableAmount : null,
                CancellationPolicyText = b.HotelBooking != null ? b.HotelBooking.CancellationPolicyText : null
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// Rezervasyon detayını getirir
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.HotelBooking)
                .ThenInclude(hb => hb!.Hotel)
            .Include(b => b.Payment)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        return Ok(new BookingDetailDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            Type = booking.Type.ToString(),
            Status = booking.Status.ToString(),
            TotalPrice = booking.TotalPrice,
            Commission = booking.Commission,
            Currency = booking.Currency,
            CouponDiscount = booking.CouponDiscount,
            Notes = booking.Notes,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            HotelBooking = booking.HotelBooking != null ? new HotelBookingDetailDto
            {
                // Frontend beklenen alanlar
                HotelName = booking.HotelBooking.Hotel?.Name ?? booking.HotelBooking.RoomTypeName, // Hotel adı veya fallback
                RoomTypeName = booking.HotelBooking.RoomTypeName,
                BoardTypeName = booking.HotelBooking.MealName, // Alias
                Rooms = 1, // Şimdilik sabit, entity'de yok
                CheckIn = booking.HotelBooking.CheckIn,
                CheckOut = booking.HotelBooking.CheckOut,
                Adults = booking.HotelBooking.Adults,
                Children = booking.HotelBooking.Children,
                GuestName = booking.HotelBooking.GuestName,
                GuestEmail = booking.HotelBooking.GuestEmail,
                GuestPhone = booking.HotelBooking.GuestPhone,
                SpecialRequests = booking.HotelBooking.SpecialRequests,
                SunhotelsBookingCode = booking.HotelBooking.ConfirmationCode, // Alias
                HotelConfirmationNumber = null, // Opsiyonel, entity'de yok
                TotalPrice = booking.TotalPrice, // Booking'den al
                Currency = booking.Currency,
                TaxAmount = null, // Opsiyonel, entity'de yok

                // Ek detaylar (admin için)
                Id = booking.HotelBooking.Id,
                ExternalHotelId = booking.HotelBooking.ExternalHotelId,
                RoomId = booking.HotelBooking.RoomId,
                RoomTypeId = booking.HotelBooking.RoomTypeId,
                MealId = booking.HotelBooking.MealId,
                MealName = booking.HotelBooking.MealName,
                PreBookCode = booking.HotelBooking.PreBookCode,
                ConfirmationCode = booking.HotelBooking.ConfirmationCode,
                Voucher = booking.HotelBooking.Voucher,
                InvoiceRef = booking.HotelBooking.InvoiceRef,
                HotelAddress = booking.HotelBooking.HotelAddress,
                HotelPhone = booking.HotelBooking.HotelPhone,
                HotelNotes = booking.HotelBooking.HotelNotes,
                CancellationPolicies = booking.HotelBooking.CancellationPolicies,
                IsSuperDeal = booking.HotelBooking.IsSuperDeal,
                SunHotelsBookingDate = booking.HotelBooking.SunHotelsBookingDate,
                ConfirmationEmailSent = booking.HotelBooking.ConfirmationEmailSent,
                ConfirmationEmailSentAt = booking.HotelBooking.ConfirmationEmailSentAt,

                // Cancellation & Refund Info
                IsRefundable = booking.HotelBooking.IsRefundable,
                FreeCancellationDeadline = booking.HotelBooking.FreeCancellationDeadline,
                CancellationPercentage = booking.HotelBooking.CancellationPercentage,
                MaxRefundableAmount = booking.HotelBooking.MaxRefundableAmount,
                CancellationPolicyText = booking.HotelBooking.CancellationPolicyText
            } : null,
            Payment = booking.Payment != null ? new PaymentDetailDto
            {
                // Frontend beklenen alanlar
                Status = booking.Payment.Status.ToString(),
                PaidAt = booking.Payment.PaidAt,
                StripeSessionId = null, // Entity'de yok, checkout session id
                StripePaymentIntentId = booking.Payment.StripePaymentIntentId,
                Amount = booking.Payment.Amount,
                Currency = booking.Payment.Currency,

                // Ek detaylar (admin için)
                Id = booking.Payment.Id,
                StripePaymentId = booking.Payment.StripePaymentId,
                FailureReason = booking.Payment.FailureReason
            } : null
        });
    }

    #endregion

    #region Retry SunHotels Booking

    /// <summary>
    /// Başarısız SunHotels rezervasyonunu yeniden gönderir
    /// </summary>
    [HttpPost("{id:guid}/retry-sunhotels")]
    public async Task<IActionResult> RetrySunHotelsBooking(
        Guid id,
        [FromBody] RetrySunHotelsRequest? request,
        CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.HotelBooking)
            .Include(b => b.Payment)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.HotelBooking == null)
            return BadRequest(new { message = "This booking has no hotel booking details" });

        if (string.IsNullOrEmpty(booking.HotelBooking.PreBookCode))
            return BadRequest(new { message = "PreBookCode is missing. Cannot retry SunHotels booking." });

        // Zaten başarılı bir rezervasyon varsa uyar
        if (!string.IsNullOrEmpty(booking.HotelBooking.ConfirmationCode) &&
            booking.Status == BookingStatus.Confirmed)
        {
            return BadRequest(new { message = "This booking is already confirmed with SunHotels", confirmationCode = booking.HotelBooking.ConfirmationCode });
        }

        _logger.LogInformation("Admin retrying SunHotels booking - BookingId: {BookingId}, PreBookCode: {PreBookCode}",
            id, booking.HotelBooking.PreBookCode);

        try
        {
            // Guest bilgilerini al
            var adultGuestFirstName = booking.HotelBooking.GuestName?.Split(' ').FirstOrDefault() ?? "Guest";
            var adultGuestLastName = booking.HotelBooking.GuestName?.Split(' ').LastOrDefault() ?? "User";
            var customerCountry = request?.CustomerCountry ?? "TR";

            // SunHotels BookV3 isteği oluştur
            var bookV3Request = new SunHotelsBookRequestV3
            {
                PreBookCode = booking.HotelBooking.PreBookCode,
                RoomId = booking.HotelBooking.RoomId,
                MealId = booking.HotelBooking.MealId,
                CheckIn = booking.HotelBooking.CheckIn,
                CheckOut = booking.HotelBooking.CheckOut,
                Rooms = 1,
                Adults = booking.HotelBooking.Adults,
                Children = booking.HotelBooking.Children,
                Infant = 0,
                Currency = booking.Currency,
                Language = "en",
                Email = booking.HotelBooking.GuestEmail ?? throw new InvalidOperationException("Guest email is required"),
                YourRef = $"FS-{booking.Id}",
                CustomerCountry = customerCountry,
                SpecialRequest = booking.HotelBooking.SpecialRequests ?? "",
                PaymentMethodId = 0,
                B2C = booking.HotelBooking.IsSuperDeal,
                AdultGuests = GenerateAdultGuestList(booking.HotelBooking.Adults, adultGuestFirstName, adultGuestLastName)
            };

            // SunHotels BookV3 çağrısı
            var bookResult = await _sunHotelsService.BookV3Async(bookV3Request, cancellationToken);

            // Başarılı - verileri güncelle
            var now = DateTime.UtcNow;

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = now;
            booking.Notes = (booking.Notes ?? "") + $"\n[Admin Retry] SunHotels booking confirmed at {now:yyyy-MM-dd HH:mm:ss} UTC - {bookResult.BookingNumber}";

            booking.HotelBooking.ConfirmationCode = bookResult.BookingNumber;
            booking.HotelBooking.Voucher = bookResult.Voucher;
            booking.HotelBooking.InvoiceRef = bookResult.InvoiceRef;
            booking.HotelBooking.HotelAddress = bookResult.HotelAddress;
            booking.HotelBooking.HotelPhone = bookResult.HotelPhone;
            booking.HotelBooking.MealName = bookResult.MealName;

            // BookingDate'i UTC'ye çevir
            var bd = bookResult.BookingDate;
            booking.HotelBooking.SunHotelsBookingDate = bd.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(bd, DateTimeKind.Utc)
                : bd.ToUniversalTime();

            // Cancellation policies
            if (bookResult.CancellationPolicyTexts?.Any() == true)
            {
                booking.HotelBooking.CancellationPolicies = JsonSerializer.Serialize(bookResult.CancellationPolicyTexts);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Admin successfully retried SunHotels booking - BookingId: {BookingId}, ConfirmationCode: {ConfirmationCode}",
                id, bookResult.BookingNumber);

            // Onay emaili gönder (isteğe bağlı)
            if (request?.SendConfirmationEmail == true)
            {
                try
                {
                    await SendConfirmationEmailAsync(booking, bookResult, cancellationToken);
                    booking.HotelBooking.ConfirmationEmailSent = true;
                    booking.HotelBooking.ConfirmationEmailSentAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send confirmation email for booking {BookingId}", id);
                }
            }

            return Ok(new
            {
                success = true,
                message = "SunHotels booking confirmed successfully",
                confirmationCode = bookResult.BookingNumber,
                voucher = bookResult.Voucher,
                bookingStatus = booking.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin retry SunHotels booking failed - BookingId: {BookingId}", id);

            // Hata notunu kaydet
            booking.Notes = (booking.Notes ?? "") + $"\n[Admin Retry Failed] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - Error: {ex.Message}";
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BadRequest(new
            {
                success = false,
                message = "SunHotels booking failed",
                error = ex.Message
            });
        }
    }

    #endregion

    #region Refund

    /// <summary>
    /// Stripe üzerinden tam veya kısmi iade yapar
    /// İptal politikasını kontrol ederek uyarı verir
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> RefundBooking(
        Guid id,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.HotelBooking)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.Payment == null)
            return BadRequest(new { message = "No payment record found for this booking" });

        if (string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId))
            return BadRequest(new { message = "Stripe PaymentIntentId is missing. Cannot process refund." });

        if (booking.Payment.Status != PaymentStatus.Completed)
            return BadRequest(new { message = "Payment is not in completed status", currentStatus = booking.Payment.Status.ToString() });

        // İptal politikası kontrolü
        var hotelBooking = booking.HotelBooking;
        var refundWarning = "";
        decimal recommendedRefundAmount = booking.Payment.Amount;
        bool isNonRefundable = false;

        if (hotelBooking != null)
        {
            if (!hotelBooking.IsRefundable)
            {
                isNonRefundable = true;
                refundWarning = $"⚠️ WARNING: This booking is NON-REFUNDABLE. SunHotels will charge 100% cancellation fee. Policy: {hotelBooking.CancellationPolicyText}";
                recommendedRefundAmount = 0;
            }
            else if (hotelBooking.FreeCancellationDeadline.HasValue && DateTime.UtcNow > hotelBooking.FreeCancellationDeadline.Value)
            {
                // Ücretsiz iptal süresi geçmiş
                var cancellationFeeAmount = booking.Payment.Amount * (hotelBooking.CancellationPercentage / 100);
                recommendedRefundAmount = booking.Payment.Amount - cancellationFeeAmount;
                refundWarning = $"⚠️ WARNING: Free cancellation deadline has passed ({hotelBooking.FreeCancellationDeadline.Value:dd MMM yyyy}). Cancellation fee: {hotelBooking.CancellationPercentage}% = {cancellationFeeAmount:F2} {booking.Currency}. Recommended refund: {recommendedRefundAmount:F2} {booking.Currency}";
            }
            else if (hotelBooking.MaxRefundableAmount.HasValue && hotelBooking.MaxRefundableAmount.Value < booking.Payment.Amount)
            {
                recommendedRefundAmount = hotelBooking.MaxRefundableAmount.Value;
                refundWarning = $"Maximum refundable amount based on policy: {recommendedRefundAmount:F2} {booking.Currency}";
            }
        }

        // Eğer admin forceRefund=false göndermediyse ve non-refundable ise işlemi durdur
        if (isNonRefundable && request.ForceRefund != true)
        {
            return BadRequest(new
            {
                success = false,
                message = "Booking is non-refundable. Add 'forceRefund: true' to process anyway.",
                warning = refundWarning,
                recommendedRefundAmount = 0,
                cancellationPolicy = hotelBooking?.CancellationPolicyText
            });
        }

        _logger.LogInformation("Admin initiating refund - BookingId: {BookingId}, PaymentIntentId: {PaymentIntentId}, Amount: {Amount}, Warning: {Warning}",
            id, booking.Payment.StripePaymentIntentId, request.Amount, refundWarning);

        try
        {
            // Stripe API key ayarla
            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
                return StatusCode(500, new { message = "Stripe is not configured" });

            StripeConfiguration.ApiKey = stripeSecretKey;

            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = booking.Payment.StripePaymentIntentId,
                Reason = request.Reason switch
                {
                    "duplicate" => RefundReasons.Duplicate,
                    "fraudulent" => RefundReasons.Fraudulent,
                    _ => RefundReasons.RequestedByCustomer
                },
                Metadata = new Dictionary<string, string>
                {
                    { "booking_id", booking.Id.ToString() },
                    { "admin_note", request.AdminNote ?? "" },
                    { "refunded_by", "admin" }
                }
            };

            // Kısmi iade mi?
            if (request.Amount.HasValue && request.Amount.Value > 0)
            {
                // Stripe tutarı cent/kuruş olarak bekler
                refundOptions.Amount = (long)(request.Amount.Value * 100);
            }

            var refund = await refundService.CreateAsync(refundOptions, cancellationToken: cancellationToken);

            // Başarılı - verileri güncelle
            var now = DateTime.UtcNow;
            var refundAmount = refund.Amount / 100m;
            var isFullRefund = !request.Amount.HasValue || request.Amount.Value >= booking.Payment.Amount;

            booking.Status = isFullRefund ? BookingStatus.Refunded : booking.Status;
            booking.UpdatedAt = now;
            booking.Notes = (booking.Notes ?? "") + $"\n[Refund] {now:yyyy-MM-dd HH:mm:ss} UTC - Amount: {refundAmount} {refund.Currency.ToUpper()}, RefundId: {refund.Id}, Reason: {request.Reason ?? "requested_by_customer"}";

            if (isFullRefund)
            {
                booking.Payment.Status = PaymentStatus.Refunded;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refund successful - BookingId: {BookingId}, RefundId: {RefundId}, Amount: {Amount}",
                id, refund.Id, refundAmount);

            // İade emaili gönder (isteğe bağlı)
            if (request.SendRefundEmail == true && booking.HotelBooking?.GuestEmail != null)
            {
                try
                {
                    await SendRefundEmailAsync(booking, refundAmount, refund.Currency.ToUpper(), request.AdminNote, cancellationToken);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send refund email for booking {BookingId}", id);
                }
            }

            return Ok(new
            {
                success = true,
                message = isFullRefund ? "Full refund processed successfully" : "Partial refund processed successfully",
                refundId = refund.Id,
                refundAmount = refundAmount,
                currency = refund.Currency.ToUpper(),
                refundStatus = refund.Status,
                bookingStatus = booking.Status.ToString(),
                warning = string.IsNullOrEmpty(refundWarning) ? null : refundWarning,
                policyInfo = new
                {
                    isRefundable = hotelBooking?.IsRefundable ?? true,
                    freeCancellationDeadline = hotelBooking?.FreeCancellationDeadline,
                    cancellationPercentage = hotelBooking?.CancellationPercentage ?? 0,
                    maxRefundableAmount = hotelBooking?.MaxRefundableAmount,
                    policyText = hotelBooking?.CancellationPolicyText
                }
            });
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe refund failed - BookingId: {BookingId}", id);

            return BadRequest(new
            {
                success = false,
                message = "Stripe refund failed",
                error = stripeEx.StripeError?.Message ?? stripeEx.Message,
                code = stripeEx.StripeError?.Code
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refund failed - BookingId: {BookingId}", id);

            return BadRequest(new
            {
                success = false,
                message = "Refund failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Rezervasyonun iade durumunu kontrol eder
    /// </summary>
    [HttpGet("{id:guid}/refund-status")]
    public async Task<IActionResult> GetRefundStatus(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.Payment == null || string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId))
            return BadRequest(new { message = "No Stripe payment found for this booking" });

        try
        {
            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
                return StatusCode(500, new { message = "Stripe is not configured" });

            StripeConfiguration.ApiKey = stripeSecretKey;

            var refundService = new RefundService();
            var refunds = await refundService.ListAsync(new RefundListOptions
            {
                PaymentIntent = booking.Payment.StripePaymentIntentId
            }, cancellationToken: cancellationToken);

            var refundList = refunds.Data.Select(r => new
            {
                refundId = r.Id,
                amount = r.Amount / 100m,
                currency = r.Currency.ToUpper(),
                status = r.Status,
                reason = r.Reason,
                createdAt = r.Created
            }).ToList();

            var totalRefunded = refunds.Data.Where(r => r.Status == "succeeded").Sum(r => r.Amount) / 100m;

            return Ok(new
            {
                bookingId = id,
                paymentIntentId = booking.Payment.StripePaymentIntentId,
                originalAmount = booking.Payment.Amount,
                totalRefunded,
                remainingAmount = booking.Payment.Amount - totalRefunded,
                refunds = refundList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get refund status - BookingId: {BookingId}", id);
            return BadRequest(new { message = "Failed to get refund status", error = ex.Message });
        }
    }

    #endregion

    #region Cancel SunHotels Booking

    /// <summary>
    /// SunHotels rezervasyonunu iptal eder
    /// </summary>
    [HttpPost("{id:guid}/cancel-sunhotels")]
    public async Task<IActionResult> CancelSunHotelsBooking(
        Guid id,
        [FromBody] CancelSunHotelsRequest? request,
        CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.HotelBooking)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.HotelBooking == null)
            return BadRequest(new { message = "This booking has no hotel booking details" });

        if (string.IsNullOrEmpty(booking.HotelBooking.ConfirmationCode))
            return BadRequest(new { message = "No SunHotels confirmation code found. Booking may not be confirmed yet." });

        _logger.LogInformation("Admin cancelling SunHotels booking - BookingId: {BookingId}, ConfirmationCode: {ConfirmationCode}",
            id, booking.HotelBooking.ConfirmationCode);

        try
        {
            // SunHotels CancelBooking çağrısı
            var cancelResult = await _sunHotelsService.CancelBookingAsync(
                booking.HotelBooking.ConfirmationCode,
                "en",
                cancellationToken);

            var now = DateTime.UtcNow;

            if (cancelResult.Success)
            {
                // İptal ücretlerini hesapla
                var totalCancellationFee = cancelResult.PaymentMethods
                    .SelectMany(pm => pm.CancellationFees)
                    .Sum(f => f.Amount);

                var cancellationCurrency = cancelResult.PaymentMethods
                    .SelectMany(pm => pm.CancellationFees)
                    .FirstOrDefault()?.Currency ?? "EUR";

                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = now;
                booking.Notes = (booking.Notes ?? "") + $"\n[SunHotels Cancel] {now:yyyy-MM-dd HH:mm:ss} UTC - Cancelled successfully. Cancellation Fee: {totalCancellationFee} {cancellationCurrency}";

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("SunHotels booking cancelled - BookingId: {BookingId}, CancellationFee: {Fee} {Currency}",
                    id, totalCancellationFee, cancellationCurrency);

                // İade işlemi de yapılsın mı?
                if (request?.ProcessRefund == true && booking.Payment?.StripePaymentIntentId != null)
                {
                    // İptal ücreti düşülerek iade
                    var refundAmount = booking.Payment.Amount - totalCancellationFee;
                    if (refundAmount > 0)
                    {
                        var refundRequest = new RefundRequest
                        {
                            Amount = refundAmount,
                            Reason = "requested_by_customer",
                            AdminNote = $"SunHotels cancellation. Cancellation fee: {totalCancellationFee} {cancellationCurrency}"
                        };

                        // İade işlemini çağır (recursive değil, doğrudan)
                        return await RefundBooking(id, refundRequest, cancellationToken);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "SunHotels booking cancelled successfully",
                    cancellationFee = totalCancellationFee,
                    currency = cancellationCurrency,
                    paymentMethods = cancelResult.PaymentMethods,
                    bookingStatus = booking.Status.ToString()
                });
            }
            else
            {
                booking.Notes = (booking.Notes ?? "") + $"\n[SunHotels Cancel Failed] {now:yyyy-MM-dd HH:mm:ss} UTC - Error: {cancelResult.Message}";
                await _dbContext.SaveChangesAsync(cancellationToken);

                return BadRequest(new
                {
                    success = false,
                    message = "SunHotels cancellation failed",
                    error = cancelResult.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SunHotels cancellation failed - BookingId: {BookingId}", id);
            return BadRequest(new
            {
                success = false,
                message = "SunHotels cancellation failed",
                error = ex.Message
            });
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Rezervasyon istatistiklerini getirir
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _dbContext.Bookings
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalRevenue = await _dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, cancellationToken);

        var totalRefunded = await _dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Refunded)
            .SumAsync(p => p.Amount, cancellationToken);

        var failedConfirmations = await _dbContext.Bookings
            .CountAsync(b => b.Status == BookingStatus.ConfirmationFailed, cancellationToken);

        return Ok(new
        {
            byStatus = stats,
            totalRevenue,
            totalRefunded,
            failedConfirmations,
            needsAttention = failedConfirmations
        });
    }

    #endregion

    #region Helper Methods

    private List<SunHotelsGuest> GenerateAdultGuestList(int adults, string firstName, string lastName)
    {
        var guests = new List<SunHotelsGuest>();
        for (int i = 0; i < adults; i++)
        {
            guests.Add(new SunHotelsGuest { FirstName = firstName, LastName = lastName });
        }
        return guests;
    }

    private async Task SendConfirmationEmailAsync(Booking booking, SunHotelsBookResultV3 bookResult, CancellationToken cancellationToken)
    {
        if (booking.HotelBooking?.GuestEmail == null) return;

        var emailHtml = GenerateConfirmationEmailHtml(
            booking.HotelBooking.GuestName ?? "Valued Guest",
            bookResult.BookingNumber ?? "",
            bookResult.BookingNumber ?? "",
            booking.HotelBooking.RoomTypeName ?? "Standard Room",
            booking.HotelBooking.CheckIn.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
            booking.HotelBooking.CheckOut.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
            booking.HotelBooking.Adults.ToString(),
            booking.HotelBooking.Children.ToString(),
            bookResult.MealName ?? "Not Specified",
            $"{booking.TotalPrice:N2} {booking.Currency}",
            bookResult.Voucher ?? "",
            booking.HotelBooking.SpecialRequests,
            bookResult.HotelAddress,
            bookResult.HotelPhone,
            bookResult.CancellationPolicyTexts
        );

        await _emailService.SendEmailAsync(
            booking.HotelBooking.GuestEmail,
            $"Booking Confirmation - {bookResult.BookingNumber}",
            emailHtml,
            isHtml: true,
            cancellationToken: cancellationToken
        );
    }

    private async Task SendRefundEmailAsync(Booking booking, decimal refundAmount, string currency, string? adminNote, CancellationToken cancellationToken)
    {
        if (booking.HotelBooking?.GuestEmail == null) return;

        var emailHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Refund Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #dc3545; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0;'>💰 Refund Processed</h1>
    </div>
    
    <div style='background: #f9f9f9; padding: 30px; border: 1px solid #ddd; border-top: none;'>
        <p>Dear <strong>{booking.HotelBooking.GuestName}</strong>,</p>
        
        <p>Your refund has been processed. Details below:</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p><strong>Refund Amount:</strong> {refundAmount:N2} {currency}</p>
            <p><strong>Booking Reference:</strong> {booking.HotelBooking.ConfirmationCode ?? booking.Id.ToString()}</p>
            {(!string.IsNullOrEmpty(adminNote) ? $"<p><strong>Note:</strong> {adminNote}</p>" : "")}
        </div>
        
        <p>The refund will appear in your account within 5-10 business days.</p>
        
        <p>If you have any questions, please contact us.</p>
    </div>
    
    <div style='background: #333; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px;'>
        <p style='margin: 0;'>© {DateTime.Now.Year} FreeStays</p>
    </div>
</body>
</html>";

        await _emailService.SendEmailAsync(
            booking.HotelBooking.GuestEmail,
            "Refund Confirmation - FreeStays",
            emailHtml,
            isHtml: true,
            cancellationToken: cancellationToken
        );
    }

    private string GenerateConfirmationEmailHtml(
        string guestName,
        string bookingNumber,
        string confirmationCode,
        string roomType,
        string checkIn,
        string checkOut,
        string adults,
        string children,
        string mealPlan,
        string totalPrice,
        string voucher,
        string? specialRequests,
        string? hotelAddress,
        string? hotelPhone,
        List<string>? cancellationPolicies)
    {
        // Simplified version - you can use the full template from PaymentsController
        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>Booking Confirmation</title></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <h1>🎉 Your Booking is Confirmed!</h1>
    <p>Dear {guestName},</p>
    <p>Your booking has been confirmed. Details:</p>
    <ul>
        <li><strong>Booking No:</strong> {bookingNumber}</li>
        <li><strong>Room:</strong> {roomType}</li>
        <li><strong>Check-in:</strong> {checkIn}</li>
        <li><strong>Check-out:</strong> {checkOut}</li>
        <li><strong>Guests:</strong> {adults} Adults{(children != "0" ? $", {children} Children" : "")}</li>
        <li><strong>Total:</strong> {totalPrice}</li>
    </ul>
    <p><strong>Voucher Code:</strong> {voucher}</p>
    {(!string.IsNullOrEmpty(hotelAddress) ? $"<p><strong>Hotel Address:</strong> {hotelAddress}</p>" : "")}
    {(!string.IsNullOrEmpty(hotelPhone) ? $"<p><strong>Hotel Phone:</strong> {hotelPhone}</p>" : "")}
    <p>Have a wonderful stay! 🌴</p>
</body>
</html>";
    }

    #endregion
}

#region DTOs

public class BookingListDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "";
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? HotelName { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public string? ConfirmationCode { get; set; }
    public string? PaymentStatus { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FailedConfirmationDto
{
    public Guid BookingId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public int ExternalHotelId { get; set; }
    public int RoomId { get; set; }
    public string? RoomTypeName { get; set; }
    public int MealId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? PreBookCode { get; set; }
    public bool IsSuperDeal { get; set; }
    public string? SpecialRequests { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "";
    public decimal PaymentAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanRetry { get; set; }
    public bool CanRefund { get; set; }

    // Cancellation & Refund Info
    public bool IsRefundable { get; set; }
    public DateTime? FreeCancellationDeadline { get; set; }
    public decimal CancellationPercentage { get; set; }
    public decimal? MaxRefundableAmount { get; set; }
    public string? CancellationPolicyText { get; set; }
}

public class BookingDetailDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public decimal Commission { get; set; }
    public string Currency { get; set; } = "";
    public decimal CouponDiscount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public HotelBookingDetailDto? HotelBooking { get; set; }
    public PaymentDetailDto? Payment { get; set; }
}

public class HotelBookingDetailDto
{
    // Frontend beklenen alanlar
    public string? HotelName { get; set; }
    public string? RoomTypeName { get; set; }
    public string? BoardTypeName { get; set; } // MealName alias
    public int Rooms { get; set; } = 1;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? SpecialRequests { get; set; }
    public string? SunhotelsBookingCode { get; set; } // ConfirmationCode alias
    public string? HotelConfirmationNumber { get; set; } // Opsiyonel
    public decimal? TotalPrice { get; set; }
    public string? Currency { get; set; }
    public decimal? TaxAmount { get; set; } // Opsiyonel

    // Ek detaylar (admin için)
    public Guid Id { get; set; }
    public int ExternalHotelId { get; set; }
    public int RoomId { get; set; }
    public int RoomTypeId { get; set; }
    public int MealId { get; set; }
    public string? MealName { get; set; }
    public string? PreBookCode { get; set; }
    public string? ConfirmationCode { get; set; }
    public string? Voucher { get; set; }
    public string? InvoiceRef { get; set; }
    public string? HotelAddress { get; set; }
    public string? HotelPhone { get; set; }
    public string? HotelNotes { get; set; }
    public string? CancellationPolicies { get; set; }
    public bool IsSuperDeal { get; set; }
    public DateTime? SunHotelsBookingDate { get; set; }
    public bool ConfirmationEmailSent { get; set; }
    public DateTime? ConfirmationEmailSentAt { get; set; }

    // Cancellation & Refund Info
    public bool IsRefundable { get; set; }
    public DateTime? FreeCancellationDeadline { get; set; }
    public decimal CancellationPercentage { get; set; }
    public decimal? MaxRefundableAmount { get; set; }
    public string? CancellationPolicyText { get; set; }
}

public class PaymentDetailDto
{
    public string Status { get; set; } = "";
    public DateTime? PaidAt { get; set; }
    public string? StripeSessionId { get; set; } // Stripe checkout session id
    public string? StripePaymentIntentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";

    // Ek detaylar (admin için)
    public Guid Id { get; set; }
    public string? StripePaymentId { get; set; }
    public string? FailureReason { get; set; }
}

public class RetrySunHotelsRequest
{
    public string? CustomerCountry { get; set; }
    public bool SendConfirmationEmail { get; set; } = true;
}

public class RefundRequest
{
    /// <summary>
    /// İade tutarı. Null ise tam iade yapılır.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// İade sebebi: duplicate, fraudulent, requested_by_customer
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Admin notu
    /// </summary>
    public string? AdminNote { get; set; }

    /// <summary>
    /// Müşteriye iade emaili gönderilsin mi?
    /// </summary>
    public bool SendRefundEmail { get; set; } = true;

    /// <summary>
    /// Non-refundable rezervasyonlarda iade zorla yapılsın mı?
    /// true ise politikaya rağmen iade yapılır (zarar ederiz)
    /// </summary>
    public bool ForceRefund { get; set; } = false;
}

public class CancelSunHotelsRequest
{
    /// <summary>
    /// İptal sonrası otomatik iade yapılsın mı?
    /// </summary>
    public bool ProcessRefund { get; set; } = false;
}

#endregion

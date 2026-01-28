using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers;

[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISunHotelsService _sunHotelsService;
    private readonly ILogger<PaymentsController> _logger;
    private readonly FreeStaysDbContext _dbContext;
    private readonly IEmailService _emailService;

    public PaymentsController(
        IStripePaymentService stripePaymentService,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ISunHotelsService sunHotelsService,
        ILogger<PaymentsController> logger,
        FreeStaysDbContext dbContext,
        IEmailService emailService)
    {
        _stripePaymentService = stripePaymentService;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _sunHotelsService = sunHotelsService;
        _logger = logger;
        _dbContext = dbContext;
        _emailService = emailService;
    }

    /// <summary>
    /// Ödeme başlat (PaymentIntent oluştur)
    /// Authentication gerektirmez - verification token veya user auth ile çalışır
    /// </summary>
    [HttpPost("initiate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
    {
        try
        {
            // Booking kontrolü
            var booking = await _bookingRepository.GetByIdAsync(request.BookingId);
            if (booking == null)
            {
                return NotFound(new { message = "Rezervasyon bulunamadı." });
            }

            // Yetki kontrolü: Ya authenticated user sahibi ya da verification token doğru olmalı
            var isOwner = _currentUserService.UserId.HasValue && booking.UserId == _currentUserService.UserId.Value;
            var hasValidToken = !string.IsNullOrEmpty(request.VerificationToken) &&
                                booking.VerificationToken == request.VerificationToken;

            if (!isOwner && !hasValidToken)
            {
                return Unauthorized(new { message = "Bu rezervasyon için ödeme başlatma yetkiniz yok." });
            }

            // Zaten ödenmiş mi kontrolü
            if (booking.Payment != null && booking.Payment.Status == PaymentStatus.Completed)
            {
                return BadRequest(new { message = "Bu rezervasyon zaten ödenmiş." });
            }

            // PaymentIntent oluştur
            var metadata = new Dictionary<string, string>
            {
                { "bookingType", booking.Type.ToString() },
                { "userId", booking.UserId.ToString() }
            };

            var clientSecret = await _stripePaymentService.CreatePaymentIntentAsync(
                booking.TotalPrice,
                booking.Currency,
                booking.Id,
                metadata);

            return Ok(new
            {
                clientSecret = clientSecret,
                bookingId = booking.Id,
                amount = booking.TotalPrice,
                currency = booking.Currency
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ödeme başlatılamadı.", error = ex.Message });
        }
    }

    /// <summary>
    /// Checkout Session durumunu kontrol et (Success page için)
    /// Frontend success sayfasında session_id ile bu endpoint'i çağırır
    /// Ödeme başarılıysa ve booking hala Pending ise otomatik SunHotels confirm yapılır
    /// </summary>
    [HttpGet("{sessionId}/status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCheckoutSessionStatus(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            // Checkout Session ID mi yoksa PaymentIntent ID mi kontrol et
            if (sessionId.StartsWith("cs_"))
            {
                // Checkout Session - Stripe'dan bilgileri al
                var session = await _stripePaymentService.GetCheckoutSessionAsync(sessionId, cancellationToken);

                if (session == null)
                {
                    return NotFound(new { message = "Checkout session bulunamadı." });
                }

                // Session metadata'dan booking ID'yi al
                var bookingIdStr = session.Metadata?.GetValueOrDefault("bookingId");
                Domain.Entities.Booking? booking = null;

                if (!string.IsNullOrEmpty(bookingIdStr) && Guid.TryParse(bookingIdStr, out var bookingId))
                {
                    booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, cancellationToken);
                }

                var isPaid = session.PaymentStatus == "paid";
                var isCompleted = session.Status == "complete";

                // ÖNEMLİ: Ödeme tamamlandı ve booking hala Pending ise otomatik confirm yap
                // Bu, Stripe webhook'un çalışmadığı durumlar için fallback mekanizması
                string? confirmationError = null;
                if (isPaid && isCompleted && booking != null && booking.Status == BookingStatus.Pending)
                {
                    _logger.LogInformation("Payment completed but booking still Pending - triggering auto-confirm for BookingId: {BookingId}", booking.Id);

                    try
                    {
                        await ConfirmHotelBookingAsync(booking, session.Metadata?.GetValueOrDefault("guestCountry") ?? "TR", cancellationToken);

                        // Booking'i yeniden yükle (güncel bilgilerle)
                        booking = await _bookingRepository.GetByIdWithDetailsAsync(booking.Id, cancellationToken);
                    }
                    catch (Exception confirmEx)
                    {
                        _logger.LogError(confirmEx, "Auto-confirm failed for BookingId: {BookingId}", booking.Id);
                        confirmationError = "Rezervasyon onaylanırken bir hata oluştu. Ödemeniz alındı, ekibimiz en kısa sürede sizinle iletişime geçecektir.";

                        // Booking status'u ConfirmationFailed olarak güncelle
                        booking.Status = BookingStatus.ConfirmationFailed;
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        // Booking'i yeniden yükle
                        booking = await _bookingRepository.GetByIdWithDetailsAsync(booking.Id, cancellationToken);
                    }
                }

                // Rezervasyon onay hatası varsa isCompleted false olmalı
                var isBookingConfirmed = booking?.Status == BookingStatus.Confirmed;
                var finalIsCompleted = isCompleted && isPaid && (confirmationError == null || isBookingConfirmed);

                return Ok(new
                {
                    sessionId = sessionId,
                    status = session.Status ?? "unknown",
                    paymentStatus = session.PaymentStatus ?? "unknown",
                    isPaid = isPaid,
                    isCompleted = finalIsCompleted,
                    hasConfirmationError = confirmationError != null,
                    confirmationError = confirmationError,
                    bookingId = booking?.Id,
                    bookingNumber = booking != null ? $"BK-{booking.CreatedAt:yyyy}-{booking.Id.ToString()[..6].ToUpper()}" : null,
                    hotelBookingCode = booking?.HotelBooking?.ConfirmationCode ?? booking?.HotelBooking?.ExternalBookingId,
                    hotelName = session.Metadata?.GetValueOrDefault("hotelName"),
                    checkIn = booking?.HotelBooking?.CheckIn,
                    checkOut = booking?.HotelBooking?.CheckOut,
                    guestName = booking?.HotelBooking?.GuestName,
                    guestEmail = booking?.HotelBooking?.GuestEmail,
                    totalPrice = booking?.TotalPrice ?? (session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : 0),
                    currency = booking?.Currency ?? session.Currency?.ToUpper() ?? "EUR",
                    bookingStatus = booking?.Status.ToString(),
                    paymentCompletedAt = booking?.Payment?.PaidAt,
                    message = confirmationError ?? GetStatusMessage(isPaid, isCompleted, booking)
                });
            }
            else if (sessionId.StartsWith("pi_"))
            {
                // PaymentIntent - Eski davranış
                var status = await _stripePaymentService.GetPaymentIntentStatusAsync(sessionId, cancellationToken);

                return Ok(new
                {
                    paymentIntentId = sessionId,
                    status = status.ToString(),
                    isPaid = status == PaymentIntentStatus.Succeeded
                });
            }
            else
            {
                return BadRequest(new { message = "Geçersiz session/payment ID formatı." });
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCheckoutSessionStatus error for sessionId: {SessionId}", sessionId);
            return StatusCode(500, new { message = "Ödeme durumu alınamadı.", error = ex.Message });
        }
    }

    /// <summary>
    /// Ödeme durumuna göre kullanıcıya gösterilecek mesaj
    /// </summary>
    private static string GetStatusMessage(bool isPaid, bool isCompleted, Domain.Entities.Booking? booking)
    {
        if (!isPaid)
            return "Ödeme bekleniyor.";

        if (booking == null)
            return "Ödemeniz alındı, rezervasyon bilgilerine ulaşılamadı.";

        return booking.Status switch
        {
            BookingStatus.Confirmed => "Ödemeniz başarıyla alındı. Rezervasyonunuz onaylandı.",
            BookingStatus.Pending => "Ödemeniz alındı, rezervasyon işleniyor...",
            BookingStatus.Failed => "Ödemeniz alındı ancak rezervasyon oluşturulamadı. Lütfen bizimle iletişime geçin.",
            BookingStatus.Cancelled => "Bu rezervasyon iptal edilmiş.",
            _ => "Ödemeniz alındı."
        };
    }

    /// <summary>
    /// Otel rezervasyonunu SunHotels ile onaylama (BookV3)
    /// Raw SQL kullanarak güncelleme yapılır (EF tracking sorunlarını önler)
    /// </summary>
    private async Task ConfirmHotelBookingAsync(Domain.Entities.Booking booking, string guestCountry, CancellationToken cancellationToken)
    {
        if (booking.HotelBooking == null)
        {
            _logger.LogWarning("ConfirmHotelBookingAsync: HotelBooking is null for BookingId: {BookingId}", booking.Id);
            return;
        }

        // Stripe Test Mode bilgisi (loglama için)
        var isTestMode = await _stripePaymentService.IsTestModeAsync(cancellationToken);
        _logger.LogInformation("ConfirmHotelBookingAsync - Stripe Mode: {Mode}, BookingId: {BookingId}",
            isTestMode ? "TEST" : "LIVE", booking.Id);

        string bookingNumber;
        string? voucher = null;
        string? invoiceRef = null;
        string? hotelNotes = null;
        string? cancellationPolicies = null;
        string? hotelAddress = null;
        string? hotelPhone = null;
        string? mealName = null;
        DateTime? sunHotelsBookingDate = null;

        // Her zaman gerçek SunHotels BookV3 çağrısı yap (test mode dahil)
        {
            // LIVE MODE: Gerçek SunHotels BookV3 çağrısı
            var adultGuestFirstName = booking.HotelBooking.GuestName?.Split(' ').FirstOrDefault() ?? "Guest";
            var adultGuestLastName = booking.HotelBooking.GuestName?.Split(' ').LastOrDefault() ?? "User";

            var bookV3Request = new SunHotelsBookRequestV3
            {
                PreBookCode = booking.HotelBooking.PreBookCode ?? throw new InvalidOperationException("PreBookCode is required"),
                RoomId = booking.HotelBooking.RoomId,
                MealId = booking.HotelBooking.MealId,
                CheckIn = booking.HotelBooking.CheckIn,
                CheckOut = booking.HotelBooking.CheckOut,
                Rooms = 1,
                Adults = booking.HotelBooking.Adults,
                Children = booking.HotelBooking.Children,
                Infant = 0,
                Currency = booking.Currency,
                Language = booking.User?.Locale ?? "tr",
                Email = booking.HotelBooking.GuestEmail ?? throw new InvalidOperationException("Guest email is required"),
                YourRef = $"FS-{booking.Id}",
                CustomerCountry = guestCountry,
                SpecialRequest = booking.HotelBooking.SpecialRequests ?? "",
                PaymentMethodId = 0,
                B2C = booking.HotelBooking.IsSuperDeal,
                AdultGuests = GenerateAdultGuestList(booking.HotelBooking.Adults, adultGuestFirstName, adultGuestLastName)
            };

            _logger.LogInformation("Calling SunHotels BookV3 - PreBookCode: {PreBookCode}, Adults: {Adults}, Children: {Children}",
                bookV3Request.PreBookCode, bookV3Request.Adults, bookV3Request.Children);

            var bookResult = await _sunHotelsService.BookV3Async(bookV3Request, cancellationToken);
            bookingNumber = bookResult.BookingNumber ?? "UNKNOWN";

            // BookV3 response'dan detayları al
            voucher = bookResult.Voucher;
            invoiceRef = bookResult.InvoiceRef;
            hotelAddress = bookResult.HotelAddress;
            hotelPhone = bookResult.HotelPhone;
            mealName = bookResult.MealName;
            // SunHotels booking date'i UTC'ye çevir (PostgreSQL timestamp with time zone gerektirir)
            var bd = bookResult.BookingDate;
            sunHotelsBookingDate = bd.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(bd, DateTimeKind.Utc)
                : bd.ToUniversalTime();
            _logger.LogInformation("SunHotels BookV3 completed - BookingNumber: {BookingNumber}, Voucher: {Voucher}, IsTestMode: {IsTestMode}",
                bookingNumber, voucher, isTestMode);
        }

        var now = DateTime.UtcNow;

        // Raw SQL ile booking status güncelle (EF tracking sorunlarını önler)
        await _dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE bookings SET status = @p0, updated_at = @p1 WHERE id = @p2",
            (int)BookingStatus.Confirmed, now, booking.Id);

        // Raw SQL ile hotel_bookings tüm detayları güncelle
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"UPDATE hotel_bookings SET 
                ""ConfirmationCode"" = @p0, 
                ""Voucher"" = @p1, 
                ""InvoiceRef"" = @p2,
                ""HotelNotes"" = @p3,
                ""CancellationPolicies"" = @p4,
                ""HotelAddress"" = @p5,
                ""HotelPhone"" = @p6,
                ""MealName"" = @p7,
                ""SunHotelsBookingDate"" = @p8,
                updated_at = @p9 
            WHERE booking_id = @p10",
            bookingNumber,
            voucher ?? "",
            invoiceRef ?? "",
            hotelNotes ?? "",
            cancellationPolicies ?? "",
            hotelAddress ?? "",
            hotelPhone ?? "",
            mealName ?? "",
            sunHotelsBookingDate,
            now,
            booking.Id);

        // Payment record - önce var mı kontrol et
        var existingPayment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.BookingId == booking.Id, cancellationToken);

        if (existingPayment == null)
        {
            // Yeni payment oluştur
            var newPayment = new Domain.Entities.Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Amount = booking.TotalPrice,
                Currency = booking.Currency,
                Status = PaymentStatus.Completed,
                PaidAt = now,
                CreatedAt = now
            };
            _dbContext.Payments.Add(newPayment);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Mevcut payment'ı güncelle
            await _dbContext.Database.ExecuteSqlRawAsync(
                "UPDATE payments SET status = @p0, paid_at = @p1, updated_at = @p2 WHERE booking_id = @p3",
                (int)PaymentStatus.Completed, now, now, booking.Id);
        }

        _logger.LogInformation("Booking confirmed successfully - BookingId: {BookingId}, ConfirmationCode: {ConfirmationCode}, IsTestMode: {IsTestMode}",
            booking.Id, bookingNumber, isTestMode);

        // Confirmation email gönder - BookV3 response detaylarıyla
        await SendConfirmationEmailAsync(
            booking,
            bookingNumber,
            voucher,
            mealName,
            hotelAddress,
            hotelPhone,
            cancellationPolicies,
            cancellationToken);
    }

    /// <summary>
    /// Rezervasyon onay emaili gönder
    /// </summary>
    private async Task SendConfirmationEmailAsync(
        Domain.Entities.Booking booking,
        string confirmationCode,
        string? voucher,
        string? mealName,
        string? hotelAddress,
        string? hotelPhone,
        string? cancellationPolicies,
        CancellationToken cancellationToken)
    {
        try
        {
            var guestEmail = booking.HotelBooking?.GuestEmail;
            if (string.IsNullOrEmpty(guestEmail))
            {
                _logger.LogWarning("Cannot send confirmation email - guest email is empty for BookingId: {BookingId}", booking.Id);
                return;
            }

            var guestName = booking.HotelBooking?.GuestName ?? "Valued Guest";
            var bookingNumber = $"BK-{booking.CreatedAt:yyyy}-{booking.Id.ToString()[..6].ToUpper()}";
            var checkIn = booking.HotelBooking?.CheckIn.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "";
            var checkOut = booking.HotelBooking?.CheckOut.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "";
            var adults = booking.HotelBooking?.Adults.ToString() ?? "0";
            var children = booking.HotelBooking?.Children.ToString() ?? "0";
            var totalPrice = $"{booking.TotalPrice:N2} {booking.Currency}";
            var voucherCode = voucher ?? confirmationCode;
            var roomTypeName = booking.HotelBooking?.RoomTypeName ?? "Standard Room";

            // Cancellation policies'i parse et
            List<string>? cancellationPolicyList = null;
            if (!string.IsNullOrEmpty(cancellationPolicies))
            {
                try
                {
                    cancellationPolicyList = JsonSerializer.Deserialize<List<string>>(cancellationPolicies);
                }
                catch { /* JSON parse hatası - ignore */ }
            }

            var subject = $"Booking Confirmation - {bookingNumber}";
            var htmlBody = GenerateConfirmationEmailHtml(
                guestName, bookingNumber, confirmationCode, checkIn, checkOut,
                adults, children, totalPrice, mealName ?? "Not Specified", voucherCode,
                roomTypeName, hotelAddress, hotelPhone, cancellationPolicyList,
                booking.HotelBooking?.SpecialRequests);

            // HTML email gönder
            await _emailService.SendGenericEmailAsync(guestEmail, subject, htmlBody, cancellationToken);

            // Email gönderildi olarak işaretle
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE hotel_bookings SET 
                    ""ConfirmationEmailSent"" = @p0, 
                    ""ConfirmationEmailSentAt"" = @p1 
                WHERE booking_id = @p2",
                true, DateTime.UtcNow, booking.Id);

            _logger.LogInformation("Confirmation email sent successfully to {Email} for BookingId: {BookingId}",
                guestEmail, booking.Id);
        }
        catch (Exception ex)
        {
            // Email hatası booking'i engellemez, sadece logla
            _logger.LogError(ex, "Failed to send confirmation email for BookingId: {BookingId}", booking.Id);
        }
    }

    /// <summary>
    /// Rezervasyon onay emaili HTML içeriği oluştur
    /// </summary>
    private static string GenerateConfirmationEmailHtml(
        string guestName, string bookingNumber, string confirmationCode,
        string checkIn, string checkOut, string adults, string children,
        string totalPrice, string mealPlan, string voucher, string roomType,
        string? hotelAddress, string? hotelPhone, List<string>? cancellationPolicies,
        string? specialRequests)
    {
        // Hotel information section
        var hotelInfoSection = "";
        if (!string.IsNullOrEmpty(hotelAddress) || !string.IsNullOrEmpty(hotelPhone))
        {
            hotelInfoSection = $@"
        <div style='background: #e8f4fd; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #0056b3; margin-top: 0;'>🏨 Hotel Information</h3>
            {(!string.IsNullOrEmpty(hotelAddress) ? $"<p style='margin: 5px 0;'><strong>Address:</strong> {hotelAddress}</p>" : "")}
            {(!string.IsNullOrEmpty(hotelPhone) ? $"<p style='margin: 5px 0;'><strong>Phone:</strong> {hotelPhone}</p>" : "")}
        </div>";
        }

        // Cancellation policy section
        var cancellationSection = "";
        if (cancellationPolicies?.Any() == true)
        {
            var policyItems = string.Join("", cancellationPolicies.Select(p => $"<li style='margin: 5px 0;'>{p}</li>"));
            cancellationSection = $@"
        <div style='background: #fff3cd; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
            <h3 style='color: #856404; margin-top: 0;'>⚠️ Cancellation Policy</h3>
            <ul style='margin: 0; padding-left: 20px;'>
                {policyItems}
            </ul>
        </div>";
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Booking Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>🎉 Your Booking is Confirmed!</h1>
    </div>
    
    <div style='background: #f9f9f9; padding: 30px; border: 1px solid #ddd; border-top: none;'>
        <p style='font-size: 18px;'>Dear <strong>{guestName}</strong>,</p>
        
        <p>Your booking has been successfully confirmed. Please find your reservation details below:</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea;'>
            <h2 style='color: #667eea; margin-top: 0;'>📋 Booking Details</h2>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'><strong>Booking No:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>{bookingNumber}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'><strong>Confirmation Code:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee; color: #667eea; font-weight: bold;'>{confirmationCode}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'><strong>Room Type:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>{roomType}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'><strong>Check-in Date:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>{checkIn}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'><strong>Check-out Date:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>{checkOut}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'><strong>Guests:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>{adults} Adult(s){(children != "0" ? $", {children} Child(ren)" : "")}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'><strong>Meal Plan:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #eee;'>{mealPlan}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0;'><strong>Total Price:</strong></td>
                    <td style='padding: 10px 0; font-size: 20px; color: #28a745; font-weight: bold;'>{totalPrice}</td>
                </tr>
            </table>
        </div>

        <div style='background: #d4edda; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745;'>
            <h3 style='color: #155724; margin-top: 0;'>🎫 Voucher Code</h3>
            <p style='font-size: 24px; font-family: monospace; background: white; padding: 15px; text-align: center; border-radius: 4px; margin: 10px 0; letter-spacing: 2px;'>
                <strong>{voucher}</strong>
            </p>
            <p style='font-size: 12px; color: #155724; margin-bottom: 0;'>Please present this code at the hotel check-in.</p>
        </div>

        {hotelInfoSection}

        {cancellationSection}

        {(!string.IsNullOrEmpty(specialRequests) ? $@"
        <div style='background: #e7f3ff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #0056b3; margin-top: 0;'>📝 Special Requests</h3>
            <p style='margin-bottom: 0;'>{specialRequests}</p>
        </div>
        " : "")}

        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;'>
            <p style='font-size: 14px; color: #666;'>
                If you have any questions, please don't hesitate to contact us.<br>
                Have a wonderful stay! 🌴
            </p>
        </div>
    </div>
    
    <div style='background: #333; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px;'>
        <p style='margin: 0; font-size: 14px;'>© {DateTime.Now.Year} FreeStays - All rights reserved.</p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Adult guest listesi oluştur
    /// </summary>
    private static List<SunHotelsGuest> GenerateAdultGuestList(int adults, string firstName, string lastName)
    {
        var guests = new List<SunHotelsGuest>();
        for (int i = 0; i < adults; i++)
        {
            guests.Add(new SunHotelsGuest
            {
                FirstName = i == 0 ? firstName : $"Guest{i + 1}",
                LastName = i == 0 ? lastName : "User"
            });
        }
        return guests;
    }

    /// <summary>
    /// Ödeme iade et
    /// </summary>
    [HttpPost("{paymentIntentId}/refund")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefundPayment(string paymentIntentId, [FromBody] RefundRequest? request)
    {
        try
        {
            var refundId = await _stripePaymentService.CreateRefundAsync(
                paymentIntentId,
                null,
                request?.Reason ?? "Admin istedi");

            return Ok(new
            {
                refundId = refundId,
                message = "İade işlemi başlatıldı."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "İade işlemi başlatılamadı.", error = ex.Message });
        }
    }

    /// <summary>
    /// Ödeme geçmişini getir
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized();
            }

            var bookings = await _bookingRepository.GetByUserIdAsync(_currentUserService.UserId.Value);
            var paymentsQuery = bookings
                .Where(b => b.Payment != null)
                .Select(b => new
                {
                    bookingId = b.Id,
                    paymentId = b.Payment!.Id,
                    amount = b.Payment.Amount,
                    currency = b.Payment.Currency,
                    status = b.Payment.Status.ToString(),
                    paidAt = b.Payment.PaidAt,
                    createdAt = b.Payment.CreatedAt,
                    bookingType = b.Type.ToString(),
                    stripePaymentIntentId = b.Payment.StripePaymentIntentId
                })
                .OrderByDescending(p => p.createdAt);

            var totalCount = paymentsQuery.Count();
            var items = paymentsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                items = items,
                page = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ödeme geçmişi alınamadı.", error = ex.Message });
        }
    }
}

public record InitiatePaymentRequest(Guid BookingId, string? VerificationToken = null);
public record RefundRequest(string Reason);

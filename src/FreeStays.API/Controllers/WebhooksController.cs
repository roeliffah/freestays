using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.Features.Coupons.Commands;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : BaseApiController
{
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhooksController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly FreeStaysDbContext _dbContext;

    public WebhooksController(
        IStripePaymentService stripePaymentService,
        IBookingRepository bookingRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<WebhooksController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        FreeStaysDbContext dbContext)
    {
        _stripePaymentService = stripePaymentService;
        _bookingRepository = bookingRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            // Read raw request body
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            // Get Stripe signature header
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe webhook received without signature");
                return BadRequest(new { message = "Missing Stripe signature" });
            }

            // Verify webhook signature
            if (!_stripePaymentService.VerifyWebhookSignature(payload, signature, out var eventType, out var data))
            {
                _logger.LogWarning("Stripe webhook signature verification failed");
                return BadRequest(new { message = "Invalid signature" });
            }

            _logger.LogInformation("Stripe webhook received: {EventType}", eventType);

            // Handle specific events
            switch (eventType)
            {
                // README: checkout.session.completed - Stripe Checkout başarılı
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(data);
                    break;

                case "payment_intent.succeeded":
                    await HandlePaymentSucceededAsync(data);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentFailedAsync(data);
                    break;

                case "charge.refunded":
                    await HandleRefundAsync(data);
                    break;

                // After-Sale: Checkout session expired without payment
                case "checkout.session.expired":
                    await HandleCheckoutSessionExpiredAsync(data);
                    break;

                // After-Sale: Async payment failed (delayed payment methods)
                case "checkout.session.async_payment_failed":
                    await HandleAsyncPaymentFailedAsync(data);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event: {EventType}", eventType);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing failed");
            return StatusCode(500, new { message = "Webhook processing failed" });
        }
    }

    /// <summary>
    /// README: checkout.session.completed event handler
    /// Stripe Checkout başarılı olduğunda çağrılır
    /// </summary>
    private async Task HandleCheckoutSessionCompletedAsync(Dictionary<string, object>? data)
    {
        if (data == null)
        {
            _logger.LogWarning("Checkout session completed webhook received with null data");
            return;
        }

        try
        {
            // Session ID ve metadata al
            var sessionId = data.GetValueOrDefault("id")?.ToString();
            var metadata = data.GetValueOrDefault("metadata") as Dictionary<string, object>;
            var customerEmail = data.GetValueOrDefault("customer_email")?.ToString();

            // Metadata'dan bilgileri al
            var preBookCode = metadata?.GetValueOrDefault("preBookCode")?.ToString();
            var bookingIdStr = metadata?.GetValueOrDefault("bookingId")?.ToString();
            var guestName = metadata?.GetValueOrDefault("guestName")?.ToString();
            var guestCountry = metadata?.GetValueOrDefault("guestCountry")?.ToString() ?? "TR";

            _logger.LogInformation("Checkout session completed - SessionId: {SessionId}, PreBookCode: {PreBookCode}, BookingId: {BookingId}",
                sessionId, preBookCode, bookingIdStr);

            // BookingId ile booking bul
            Booking? booking = null;
            if (!string.IsNullOrEmpty(bookingIdStr) && Guid.TryParse(bookingIdStr, out var bookingId))
            {
                booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId);
            }

            if (booking == null)
            {
                _logger.LogWarning("Checkout session completed but booking not found: {BookingId}", bookingIdStr);
                return;
            }

            // Ödeme başarılı - Payment kaydını güncelle
            if (booking.Payment == null)
            {
                booking.Payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    Amount = booking.TotalPrice,
                    Currency = booking.Currency
                };
            }

            booking.Payment.StripePaymentIntentId = sessionId; // Session ID'yi kaydet
            booking.Payment.Status = PaymentStatus.Completed;
            booking.Payment.PaidAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            // Otel rezervasyonu ise BookV3 çağır (SunHotels ile gerçek rezervasyon)
            if (booking.Type == BookingType.Hotel && booking.HotelBooking != null)
            {
                try
                {
                    // README: POST /api/v1/bookings/hotels/confirm çağır
                    var httpClient = _httpClientFactory.CreateClient();

                    // PreBookCode ile confirm (README formatı)
                    var confirmRequest = new
                    {
                        PreBookCode = booking.HotelBooking.PreBookCode,
                        BookingId = booking.Id,
                        GuestCountry = guestCountry
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(confirmRequest);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
                    var confirmUrl = $"{baseUrl}/api/v1/bookings/hotels/confirm";

                    _logger.LogInformation("Calling BookV3 confirmation endpoint: {Url}, PreBookCode: {PreBookCode}",
                        confirmUrl, booking.HotelBooking.PreBookCode);

                    var response = await httpClient.PostAsync(confirmUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("BookV3 confirmed for Checkout Session: {SessionId}, Response: {Response}",
                            sessionId, responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("BookV3 confirmation failed - Status: {StatusCode}, Error: {Error}",
                            response.StatusCode, errorContent);
                    }
                }
                catch (Exception bookV3Ex)
                {
                    _logger.LogError(bookV3Ex, "Failed to call BookV3 confirmation for Session: {SessionId}", sessionId);
                    // Webhook işlemi başarılı sayılır - ödeme alındı
                }
            }
            else
            {
                // Hotel dışı rezervasyonlar için sadece email gönder
                await SendBookingConfirmationEmailAsync(booking);
            }

            _logger.LogInformation("Checkout session completed processed successfully - SessionId: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle checkout session completed webhook");
        }
    }

    private async Task HandlePaymentSucceededAsync(Dictionary<string, object>? data)
    {
        if (data == null)
        {
            _logger.LogWarning("Payment succeeded webhook received with null data");
            return;
        }

        try
        {
            var paymentIntentId = data.GetValueOrDefault("id")?.ToString();
            var metadata = data.GetValueOrDefault("metadata") as Dictionary<string, object>;
            var bookingIdStr = metadata?.GetValueOrDefault("bookingId")?.ToString();

            if (string.IsNullOrEmpty(bookingIdStr) || !Guid.TryParse(bookingIdStr, out var bookingId))
            {
                _logger.LogWarning("Payment succeeded webhook: Invalid or missing bookingId");
                return;
            }

            var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId);
            if (booking == null)
            {
                _logger.LogWarning("Payment succeeded webhook: Booking not found: {BookingId}", bookingId);
                return;
            }

            // Update or create payment record
            if (booking.Payment == null)
            {
                booking.Payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingId,
                    Amount = booking.TotalPrice,
                    Currency = booking.Currency
                };
            }

            booking.Payment.StripePaymentIntentId = paymentIntentId;
            booking.Payment.Status = PaymentStatus.Completed;
            booking.Payment.PaidAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            // BookV3 reservation için guestCountry metadata'dan al
            var guestCountry = metadata?.GetValueOrDefault("guestCountry")?.ToString() ?? "TR";

            // Otel rezervasyonu ise BookV3 çağırılır
            if (booking.Type == BookingType.Hotel && booking.HotelBooking != null)
            {
                try
                {
                    // HotelBookingsController'ın ConfirmBooking method'unu çağır
                    var httpClient = _httpClientFactory.CreateClient();
                    var confirmRequest = new { BookingId = bookingId, GuestCountry = guestCountry };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(confirmRequest);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    // Base URL'yi configuration'dan al
                    var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
                    var confirmUrl = $"{baseUrl}/api/v1/bookings/hotels/confirm";

                    _logger.LogInformation("Calling BookV3 confirmation endpoint: {Url}", confirmUrl);
                    var response = await httpClient.PostAsync(confirmUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("BookV3 confirmed for Hotel Booking: {BookingId}", bookingId);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("BookV3 confirmation failed with status: {StatusCode}, Error: {Error}",
                            response.StatusCode, errorContent);
                    }
                }
                catch (Exception bookV3Ex)
                {
                    _logger.LogError(bookV3Ex, "Failed to call BookV3 confirmation for Booking: {BookingId}", bookingId);
                    // Don't throw - allow webhook processing to complete
                }
            }
            else
            {
                // Non-hotel reservations just send confirmation email
                await SendBookingConfirmationEmailAsync(booking);
            }

            _logger.LogInformation("Payment succeeded processed for Booking: {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle payment succeeded webhook");
        }
    }

    private async Task HandlePaymentFailedAsync(Dictionary<string, object>? data)
    {
        if (data == null) return;

        try
        {
            var metadata = data.GetValueOrDefault("metadata") as Dictionary<string, object>;
            var bookingIdStr = metadata?.GetValueOrDefault("bookingId")?.ToString();

            if (string.IsNullOrEmpty(bookingIdStr) || !Guid.TryParse(bookingIdStr, out var bookingId))
            {
                return;
            }

            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking?.Payment != null)
            {
                booking.Payment.Status = PaymentStatus.Failed;
                booking.Payment.FailureReason = data.GetValueOrDefault("last_payment_error")?.ToString();
                booking.Status = BookingStatus.Failed;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Payment failed processed for Booking: {BookingId}", bookingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle payment failed webhook");
        }
    }

    private async Task HandleRefundAsync(Dictionary<string, object>? data)
    {
        if (data == null) return;

        try
        {
            var paymentIntentId = data.GetValueOrDefault("payment_intent")?.ToString();
            if (string.IsNullOrEmpty(paymentIntentId)) return;

            var booking = await _bookingRepository.GetByPaymentIntentIdAsync(paymentIntentId);
            if (booking?.Payment != null)
            {
                booking.Payment.Status = PaymentStatus.Refunded;
                booking.Status = BookingStatus.Cancelled;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Refund processed for Booking: {BookingId}", booking.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle refund webhook");
        }
    }

    private async Task SendBookingConfirmationEmailAsync(Booking booking)
    {
        try
        {
            var templateCode = booking.Type switch
            {
                BookingType.Hotel => "hotel_booking_confirmed",
                BookingType.Flight => "flight_booking_confirmed",
                BookingType.Car => "car_rental_confirmed",
                _ => null
            };

            if (string.IsNullOrEmpty(templateCode))
            {
                _logger.LogWarning("Unknown booking type for email: {BookingType}", booking.Type);
                return;
            }

            var variables = new Dictionary<string, string>
            {
                { "userName", booking.User?.Name ?? "Müşteri" },
                { "bookingId", booking.Id.ToString() },
                { "totalAmount", booking.TotalPrice.ToString("N2") },
                { "currency", booking.Currency }
            };

            // Add type-specific variables
            if (booking.HotelBooking != null)
            {
                variables.Add("hotelName", booking.HotelBooking.Hotel?.Name ?? "Hotel");
                variables.Add("checkInDate", booking.HotelBooking.CheckIn.ToString("dd.MM.yyyy"));
                variables.Add("checkOutDate", booking.HotelBooking.CheckOut.ToString("dd.MM.yyyy"));
                variables.Add("guestCount", $"{booking.HotelBooking.Adults}");
                variables.Add("roomType", booking.HotelBooking.RoomTypeName ?? "Standard");
            }

            var email = booking.HotelBooking?.GuestEmail ?? booking.User?.Email;
            if (!string.IsNullOrEmpty(email))
            {
                await _emailService.SendTemplateEmailAsync(
                    email,
                    templateCode,
                    variables,
                    booking.User?.Locale ?? "tr");

                _logger.LogInformation("Confirmation email sent for Booking: {BookingId}", booking.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email for Booking: {BookingId}", booking.Id);
        }
    }

    /// <summary>
    /// After-Sale: Checkout session süresi dolduğunda çağrılır
    /// Müşteri ödeme yapmadan session'ı kapattı
    /// </summary>
    private async Task HandleCheckoutSessionExpiredAsync(Dictionary<string, object>? data)
    {
        await StoreFailedPaymentAsync(data, "expired");
    }

    /// <summary>
    /// After-Sale: Async ödeme başarısız olduğunda çağrılır (SEPA, Bancontact vb.)
    /// </summary>
    private async Task HandleAsyncPaymentFailedAsync(Dictionary<string, object>? data)
    {
        await StoreFailedPaymentAsync(data, "async_payment_failed");
    }

    /// <summary>
    /// Başarısız ödemeyi kaydeder ve after-sale takibi için saklar
    /// Python: handle_failed_payment() karşılığı
    /// </summary>
    private async Task StoreFailedPaymentAsync(Dictionary<string, object>? data, string failureType)
    {
        if (data == null) return;

        try
        {
            var sessionId = data.GetValueOrDefault("id")?.ToString();
            var metadata = data.GetValueOrDefault("metadata") as Dictionary<string, object>;
            var bookingIdStr = metadata?.GetValueOrDefault("bookingId")?.ToString();
            var customerEmail = data.GetValueOrDefault("customer_email")?.ToString();
            var customerDetails = data.GetValueOrDefault("customer_details") as Dictionary<string, object>;
            var customerName = customerDetails?.GetValueOrDefault("name")?.ToString();

            // Email from customer_details if not in root
            if (string.IsNullOrEmpty(customerEmail))
            {
                customerEmail = customerDetails?.GetValueOrDefault("email")?.ToString();
            }

            // Amount (cents to decimal)
            var amountTotal = data.GetValueOrDefault("amount_total");
            decimal amount = 0;
            if (amountTotal != null)
            {
                if (long.TryParse(amountTotal.ToString(), out var amountCents))
                {
                    amount = amountCents / 100m;
                }
            }

            var currency = data.GetValueOrDefault("currency")?.ToString()?.ToUpper() ?? "EUR";

            // FailedPayment record oluştur
            var failedPayment = new FailedPayment
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId ?? "",
                CustomerEmail = customerEmail,
                CustomerName = customerName,
                FailureType = failureType,
                Amount = amount,
                Currency = currency,
                Status = "pending",
                Metadata = System.Text.Json.JsonSerializer.Serialize(metadata)
            };

            // Booking varsa bilgilerini ekle
            if (!string.IsNullOrEmpty(bookingIdStr) && Guid.TryParse(bookingIdStr, out var bookingId))
            {
                var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId);
                if (booking != null)
                {
                    failedPayment.BookingId = bookingId;

                    // Booking durumunu güncelle
                    booking.Status = failureType == "async_payment_failed"
                        ? BookingStatus.Failed
                        : BookingStatus.Cancelled;

                    // Otel bilgilerini ekle
                    if (booking.HotelBooking != null)
                    {
                        failedPayment.HotelName = booking.HotelBooking.Hotel?.Name;
                        failedPayment.CheckIn = booking.HotelBooking.CheckIn;
                        failedPayment.CheckOut = booking.HotelBooking.CheckOut;
                        failedPayment.CustomerEmail ??= booking.HotelBooking.GuestEmail;
                        failedPayment.CustomerName ??= booking.HotelBooking.GuestName;
                    }

                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // FailedPayment kaydet
            await _dbContext.FailedPayments.AddAsync(failedPayment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Stored failed payment record - SessionId: {SessionId}, Type: {FailureType}, Email: {Email}",
                sessionId, failureType, customerEmail);

            // After-sale auto-send email (ayardan kontrol et)
            await TrySendAfterSaleEmailAsync(failedPayment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store failed payment record");
        }
    }

    /// <summary>
    /// After-sale ayarları aktifse otomatik follow-up email gönderir
    /// </summary>
    private async Task TrySendAfterSaleEmailAsync(FailedPayment failedPayment)
    {
        try
        {
            // AfterSale ayarlarını kontrol et
            var settings = await _dbContext.SiteSettings
                .Where(s => s.Group == "aftersale")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var autoSendStr = settings.GetValueOrDefault("aftersale_auto_send", "false");
            if (!bool.TryParse(autoSendStr, out var autoSend) || !autoSend)
            {
                return; // Auto-send disabled
            }

            if (string.IsNullOrEmpty(failedPayment.CustomerEmail))
            {
                _logger.LogWarning("Cannot send after-sale email - no customer email");
                return;
            }

            // Email template'i al
            var emailContent = settings.GetValueOrDefault("aftersale_email_no_payment",
                "Ödemenizin tamamlanmadığını fark ettik. Rezervasyonunuzu tamamlamak ister misiniz?");

            var customerFirstName = failedPayment.CustomerName?.Split(' ').FirstOrDefault() ?? "Değerli Müşterimiz";

            var htmlContent = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); padding: 30px; text-align: center;'>
        <h1 style='color: white; margin: 0;'>FreeStays</h1>
    </div>
    <div style='padding: 30px; background: #f9f9f9;'>
        <p>Sayın {failedPayment.CustomerName ?? "Değerli Müşterimiz"},</p>
        <p>{emailContent}</p>
        {(failedPayment.HotelName != null ? $@"
        <p><strong>Rezervasyon detaylarınız:</strong><br/>
        Otel: {failedPayment.HotelName}<br/>
        Giriş: {failedPayment.CheckIn?.ToString("dd.MM.yyyy") ?? "N/A"}<br/>
        Çıkış: {failedPayment.CheckOut?.ToString("dd.MM.yyyy") ?? "N/A"}</p>" : "")}
        <p style='margin-top: 20px;'>
            <a href='https://travelar.eu' style='background: #1e3a5f; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>FreeStays'i Ziyaret Et</a>
        </p>
        <p style='margin-top: 20px; color: #666;'>Saygılarımızla,<br/>FreeStays Ekibi</p>
    </div>
</div>";

            await _emailService.SendGenericEmailAsync(
                failedPayment.CustomerEmail,
                $"FreeStays - Sizden haber almak istiyoruz, {customerFirstName}!",
                htmlContent);

            // Kayıt güncelle
            failedPayment.Status = "contacted";
            failedPayment.ContactReason = "no_payment";
            failedPayment.ContactedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("After-sale email sent to: {Email}", failedPayment.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send after-sale email to: {Email}", failedPayment.CustomerEmail);
        }
    }
}

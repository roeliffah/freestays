using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace FreeStays.Infrastructure.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly IPaymentSettingRepository _paymentSettingRepository;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IPaymentSettingRepository paymentSettingRepository,
        ILogger<StripePaymentService> logger)
    {
        _paymentSettingRepository = paymentSettingRepository;
        _logger = logger;
    }

    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _paymentSettingRepository.GetActiveSingleAsync(cancellationToken);
        if (settings == null || !settings.IsActive)
        {
            throw new InvalidOperationException("Stripe ödeme ayarları yapılandırılmamış. Lütfen admin panelinden Stripe ayarlarını yapılandırın.");
        }

        var apiKey = settings.IsLive
            ? settings.LiveModeSecretKey ?? throw new InvalidOperationException("Stripe Live Mode secret key bulunamadı.")
            : settings.TestModeSecretKey ?? throw new InvalidOperationException("Stripe Test Mode secret key bulunamadı.");

        // Validate API key format (should start with sk_test_ or sk_live_)
        if (!apiKey.StartsWith("sk_test_") && !apiKey.StartsWith("sk_live_"))
        {
            var mode = settings.IsLive ? "Live" : "Test";
            throw new InvalidOperationException(
                $"Geçersiz Stripe {mode} Mode API key formatı. " +
                $"API key 'sk_test_' veya 'sk_live_' ile başlamalıdır. " +
                $"Lütfen admin panelinden doğru Stripe API key'i yapılandırın.");
        }

        return apiKey;
    }

    public async Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid bookingId,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(cancellationToken);
            StripeConfiguration.ApiKey = apiKey;

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Stripe uses smallest currency unit (cents for EUR/USD)
                Currency = currency.ToLower(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "bookingId", bookingId.ToString() },
                    { "source", "freestays_api" }
                }
            };

            // Add custom metadata if provided
            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    options.Metadata[item.Key] = item.Value;
                }
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe PaymentIntent created: {PaymentIntentId} for Booking: {BookingId}",
                paymentIntent.Id, bookingId);

            return paymentIntent.ClientSecret;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe PaymentIntent creation failed for Booking: {BookingId}", bookingId);
            throw new InvalidOperationException($"Ödeme başlatılamadı: {ex.Message}", ex);
        }
    }

    public async Task<PaymentIntentStatus> GetPaymentIntentStatusAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(cancellationToken);
            StripeConfiguration.ApiKey = apiKey;

            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId, cancellationToken: cancellationToken);

            return paymentIntent.Status switch
            {
                "requires_payment_method" => PaymentIntentStatus.RequiresPaymentMethod,
                "requires_confirmation" => PaymentIntentStatus.RequiresConfirmation,
                "requires_action" => PaymentIntentStatus.RequiresAction,
                "processing" => PaymentIntentStatus.Processing,
                "succeeded" => PaymentIntentStatus.Succeeded,
                "canceled" => PaymentIntentStatus.Canceled,
                "requires_capture" => PaymentIntentStatus.RequiresCapture,
                _ => PaymentIntentStatus.Unknown
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get PaymentIntent status: {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException($"Ödeme durumu alınamadı: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateRefundAsync(
        string paymentIntentId,
        decimal? amount = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(cancellationToken);
            StripeConfiguration.ApiKey = apiKey;

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Reason = reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                }
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe Refund created: {RefundId} for PaymentIntent: {PaymentIntentId}",
                refund.Id, paymentIntentId);

            return refund.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe Refund creation failed for PaymentIntent: {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException($"İade işlemi başlatılamadı: {ex.Message}", ex);
        }
    }

    public async Task<RefundStatus> GetRefundStatusAsync(string refundId, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(cancellationToken);
            StripeConfiguration.ApiKey = apiKey;

            var service = new RefundService();
            var refund = await service.GetAsync(refundId, cancellationToken: cancellationToken);

            return refund.Status switch
            {
                "pending" => RefundStatus.Pending,
                "succeeded" => RefundStatus.Succeeded,
                "failed" => RefundStatus.Failed,
                "canceled" => RefundStatus.Canceled,
                _ => RefundStatus.Unknown
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Refund status: {RefundId}", refundId);
            throw new InvalidOperationException($"İade durumu alınamadı: {ex.Message}", ex);
        }
    }

    public bool VerifyWebhookSignature(
        string payload,
        string signature,
        out string? eventType,
        out Dictionary<string, object>? data)
    {
        eventType = null;
        data = null;

        try
        {
            // Get webhook secret from settings (synchronous - can't be async in this context)
            var settings = _paymentSettingRepository.GetActiveSingleAsync().Result;
            if (settings == null || string.IsNullOrEmpty(settings.WebhookSecret))
            {
                _logger.LogError("Stripe webhook secret not configured");
                return false;
            }

            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                settings.WebhookSecret,
                throwOnApiVersionMismatch: false);

            eventType = stripeEvent.Type;
            data = stripeEvent.Data.Object as Dictionary<string, object>;

            _logger.LogInformation("Stripe webhook verified: {EventType}", eventType);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return false;
        }
    }


    public async Task<string> CreateCheckoutSessionAsync(
        decimal amount,
        string currency,
        Guid bookingId,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(cancellationToken);
            StripeConfiguration.ApiKey = apiKey;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency.ToLower(),
                            UnitAmount = (long)(amount * 100), // Stripe uses smallest currency unit
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Otel Rezervasyonu",
                                Description = $"Rezervasyon No: {bookingId}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "bookingId", bookingId.ToString() },
                    { "source", "freestays_api" }
                }
            };

            // Add custom metadata if provided
            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    options.Metadata[item.Key] = item.Value;
                }
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe Checkout Session created: {SessionId} for Booking: {BookingId}",
                session.Id, bookingId);

            return session.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe Checkout Session creation failed for Booking: {BookingId}", bookingId);
            throw new InvalidOperationException($"Ödeme oturumu oluşturulamadı: {ex.Message}", ex);
        }
    }

    public async Task<bool> IsTestModeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _paymentSettingRepository.GetActiveSingleAsync(cancellationToken);
        if (settings == null)
        {
            // Ayar yoksa test mode kabul ediyoruz
            return true;
        }

        return !settings.IsLive;
    }

    public async Task<CheckoutSessionInfo?> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(cancellationToken);
            StripeConfiguration.ApiKey = apiKey;

            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);

            if (session == null)
            {
                return null;
            }

            return new CheckoutSessionInfo
            {
                Id = session.Id,
                Status = session.Status,
                PaymentStatus = session.PaymentStatus,
                AmountTotal = session.AmountTotal,
                Currency = session.Currency,
                CustomerEmail = session.CustomerEmail,
                Metadata = session.Metadata
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe Checkout Session: {SessionId}", sessionId);
            throw new InvalidOperationException($"Checkout session bilgisi alınamadı: {ex.Message}", ex);
        }
    }
}

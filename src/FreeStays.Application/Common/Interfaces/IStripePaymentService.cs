namespace FreeStays.Application.Common.Interfaces;

public interface IStripePaymentService
{
    /// <summary>
    /// PaymentIntent oluşturur (Stripe Checkout için)
    /// </summary>
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency, Guid bookingId, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stripe Checkout Session oluşturur (Hosted Checkout sayfası için)
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        decimal amount,
        string currency,
        Guid bookingId,
        string successUrl,
        string cancelUrl,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PaymentIntent durumunu kontrol eder
    /// </summary>
    Task<PaymentIntentStatus> GetPaymentIntentStatusAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ödeme iadesini başlatır
    /// </summary>
    Task<string> CreateRefundAsync(string paymentIntentId, decimal? amount = null, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// İade durumunu kontrol eder
    /// </summary>
    Task<RefundStatus> GetRefundStatusAsync(string refundId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Webhook signature'ını doğrular
    /// </summary>
    bool VerifyWebhookSignature(string payload, string signature, out string? eventType, out Dictionary<string, object>? data);

    /// <summary>
    /// Stripe'ın test modunda olup olmadığını kontrol eder
    /// </summary>
    Task<bool> IsTestModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checkout Session bilgilerini getirir
    /// </summary>
    Task<CheckoutSessionInfo?> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Checkout Session bilgisi
/// </summary>
public class CheckoutSessionInfo
{
    public string Id { get; set; } = "";
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public long? AmountTotal { get; set; }
    public string? Currency { get; set; }
    public string? CustomerEmail { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public enum PaymentIntentStatus
{
    RequiresPaymentMethod,
    RequiresConfirmation,
    RequiresAction,
    Processing,
    Succeeded,
    Canceled,
    RequiresCapture,
    Unknown
}

public enum RefundStatus
{
    Pending,
    Succeeded,
    Failed,
    Canceled,
    Unknown
}

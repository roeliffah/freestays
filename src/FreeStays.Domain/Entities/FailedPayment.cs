using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

/// <summary>
/// Başarısız veya süresi dolmuş ödemeleri takip eder.
/// After-sale follow-up için kullanılır.
/// </summary>
public class FailedPayment : BaseEntity
{
    /// <summary>
    /// Stripe Checkout Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// İlişkili booking ID (varsa)
    /// </summary>
    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    /// <summary>
    /// Müşteri email adresi
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Müşteri adı
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Başarısızlık tipi: "async_payment_failed", "expired", "canceled"
    /// </summary>
    public string FailureType { get; set; } = string.Empty;

    /// <summary>
    /// Ödeme tutarı
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Para birimi
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Otel adı (quick reference için)
    /// </summary>
    public string? HotelName { get; set; }

    /// <summary>
    /// Giriş tarihi
    /// </summary>
    public DateTime? CheckIn { get; set; }

    /// <summary>
    /// Çıkış tarihi
    /// </summary>
    public DateTime? CheckOut { get; set; }

    /// <summary>
    /// Takip durumu: "pending", "contacted", "resolved", "not_interested"
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// İletişim nedeni: "no_payment", "stop_payment", "not_interested", "new_offers"
    /// </summary>
    public string? ContactReason { get; set; }

    /// <summary>
    /// İletişim kurulma tarihi
    /// </summary>
    public DateTime? ContactedAt { get; set; }

    /// <summary>
    /// Admin notları
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Stripe metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }
}

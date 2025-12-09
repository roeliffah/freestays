using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public string? StripePaymentId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTime? PaidAt { get; set; }
    
    // Navigation property
    public virtual Booking Booking { get; set; } = null!;
}

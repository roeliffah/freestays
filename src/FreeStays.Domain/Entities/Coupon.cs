using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public CouponKind Kind { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public decimal? MinBookingAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // Assignment
    public Guid? AssignedUserId { get; set; }
    public string? AssignedEmail { get; set; }

    // Usage tracking
    public Guid? UsedByUserId { get; set; }
    public string? UsedByEmail { get; set; }
    public DateTime? UsedAt { get; set; }

    // Pricing / sale info
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "EUR";
    public string? StripePaymentIntentId { get; set; }

    // Navigation property
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

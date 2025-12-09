using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public decimal? MinBookingAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    
    // Navigation property
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

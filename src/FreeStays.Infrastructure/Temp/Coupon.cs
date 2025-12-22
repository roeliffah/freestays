using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Coupon
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public int DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public int? MaxUses { get; set; }

    public int UsedCount { get; set; }

    public decimal? MinBookingAmount { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidUntil { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

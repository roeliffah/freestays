using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Booking
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int Type { get; set; }

    public int Status { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal Commission { get; set; }

    public string Currency { get; set; } = null!;

    public Guid? CouponId { get; set; }

    public decimal CouponDiscount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual CarRental? CarRental { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual FlightBooking? FlightBooking { get; set; }

    public virtual HotelBooking? HotelBooking { get; set; }

    public virtual Payment? Payment { get; set; }

    public virtual User User { get; set; } = null!;
}

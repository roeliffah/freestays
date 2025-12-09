using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid UserId { get; set; }
    public BookingType Type { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalPrice { get; set; }
    public decimal Commission { get; set; }
    public string Currency { get; set; } = "EUR";
    public Guid? CouponId { get; set; }
    public decimal CouponDiscount { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Coupon? Coupon { get; set; }
    public virtual HotelBooking? HotelBooking { get; set; }
    public virtual FlightBooking? FlightBooking { get; set; }
    public virtual CarRental? CarRental { get; set; }
    public virtual Payment? Payment { get; set; }
}

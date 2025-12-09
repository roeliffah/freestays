using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class HotelBooking : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid HotelId { get; set; }
    public string? RoomTypeId { get; set; }
    public string? RoomTypeName { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? ExternalBookingId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? SpecialRequests { get; set; }
    
    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;
    public virtual Hotel Hotel { get; set; } = null!;
}

using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class FlightBooking : BaseEntity
{
    public Guid BookingId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Departure { get; set; } = string.Empty;
    public string Arrival { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int Passengers { get; set; }
    public string? ExternalBookingId { get; set; }
    public string? Airline { get; set; }
    public string? Class { get; set; }
    
    // Navigation property
    public virtual Booking Booking { get; set; } = null!;
}

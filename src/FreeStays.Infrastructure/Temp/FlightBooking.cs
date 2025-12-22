using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class FlightBooking
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public string FlightNumber { get; set; } = null!;

    public string Departure { get; set; } = null!;

    public string Arrival { get; set; } = null!;

    public DateTime DepartureDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public int Passengers { get; set; }

    public string? ExternalBookingId { get; set; }

    public string? Airline { get; set; }

    public string? Class { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}

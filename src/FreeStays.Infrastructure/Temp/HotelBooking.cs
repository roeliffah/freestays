using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class HotelBooking
{
    public Guid Id { get; set; }

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

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Hotel Hotel { get; set; } = null!;
}

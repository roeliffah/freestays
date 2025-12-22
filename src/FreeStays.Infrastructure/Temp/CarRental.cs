using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class CarRental
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public string CarType { get; set; } = null!;

    public string? CarModel { get; set; }

    public string PickupLocation { get; set; } = null!;

    public string DropoffLocation { get; set; } = null!;

    public DateTime PickupDate { get; set; }

    public DateTime DropoffDate { get; set; }

    public string? ExternalBookingId { get; set; }

    public string? DriverName { get; set; }

    public string? DriverLicense { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}

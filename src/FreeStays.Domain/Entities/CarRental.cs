using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class CarRental : BaseEntity
{
    public Guid BookingId { get; set; }
    public string CarType { get; set; } = string.Empty;
    public string? CarModel { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string DropoffLocation { get; set; } = string.Empty;
    public DateTime PickupDate { get; set; }
    public DateTime DropoffDate { get; set; }
    public string? ExternalBookingId { get; set; }
    public string? DriverName { get; set; }
    public string? DriverLicense { get; set; }
    
    // Navigation property
    public virtual Booking Booking { get; set; } = null!;
}

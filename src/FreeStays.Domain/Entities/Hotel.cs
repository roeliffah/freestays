using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class Hotel : BaseEntity
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Category { get; set; } // Star rating
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? MinPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime? SyncedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<HotelImage> Images { get; set; } = new List<HotelImage>();
    public virtual ICollection<HotelFacility> Facilities { get; set; } = new List<HotelFacility>();
    public virtual ICollection<HotelBooking> HotelBookings { get; set; } = new List<HotelBooking>();
}

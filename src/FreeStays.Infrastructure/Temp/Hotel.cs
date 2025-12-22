using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Hotel
{
    public Guid Id { get; set; }

    public string ExternalId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Address { get; set; }

    public string City { get; set; } = null!;

    public string Country { get; set; } = null!;

    public int Category { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public decimal? MinPrice { get; set; }

    public string Currency { get; set; } = null!;

    public DateTime? SyncedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<FeaturedHotel> FeaturedHotels { get; set; } = new List<FeaturedHotel>();

    public virtual ICollection<HotelBooking> HotelBookings { get; set; } = new List<HotelBooking>();

    public virtual ICollection<HotelFacility> HotelFacilities { get; set; } = new List<HotelFacility>();

    public virtual ICollection<HotelImage> HotelImages { get; set; } = new List<HotelImage>();
}

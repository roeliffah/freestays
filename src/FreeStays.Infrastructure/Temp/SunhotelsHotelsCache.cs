using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class SunhotelsHotelsCache
{
    public Guid Id { get; set; }

    public int HotelId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? ZipCode { get; set; }

    public string City { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public int Category { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? GiataCode { get; set; }

    public int ResortId { get; set; }

    public string ResortName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Fax { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string FeatureIds { get; set; } = null!;

    public string ThemeIds { get; set; } = null!;

    public string ImageUrls { get; set; } = null!;

    public string Language { get; set; } = null!;

    public DateTime LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<SunhotelsRoomsCache> SunhotelsRoomsCaches { get; set; } = new List<SunhotelsRoomsCache>();
}

using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class SunhotelsRoomsCache
{
    public Guid Id { get; set; }

    public Guid HotelCacheId { get; set; }

    public int HotelId { get; set; }

    public int RoomTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string EnglishName { get; set; } = null!;

    public string? Description { get; set; }

    public int MaxOccupancy { get; set; }

    public int MinOccupancy { get; set; }

    public string FeatureIds { get; set; } = null!;

    public string ImageUrls { get; set; } = null!;

    public string Language { get; set; } = null!;

    public DateTime LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual SunhotelsHotelsCache HotelCache { get; set; } = null!;
}

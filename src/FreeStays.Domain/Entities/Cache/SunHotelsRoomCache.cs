using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Statik Oda Cache Entity
/// </summary>
public class SunHotelsRoomCache : BaseEntity
{
    public Guid HotelCacheId { get; set; }
    public int HotelId { get; set; }
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxOccupancy { get; set; }
    public int MinOccupancy { get; set; }
    
    /// <summary>
    /// Feature ID'leri JSON array olarak saklanır: "[1,2,3,4]"
    /// </summary>
    public string FeatureIds { get; set; } = "[]";
    
    /// <summary>
    /// Resim URL'leri JSON array olarak saklanır
    /// </summary>
    public string ImageUrls { get; set; } = "[]";
    
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
    
    // Navigation
    public virtual SunHotelsHotelCache? HotelCache { get; set; }
}

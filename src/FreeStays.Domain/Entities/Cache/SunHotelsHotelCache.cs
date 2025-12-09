using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Statik Otel Cache Entity
/// </summary>
public class SunHotelsHotelCache : BaseEntity
{
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public int Category { get; set; } // Yıldız sayısı
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GiataCode { get; set; }
    public int ResortId { get; set; }
    public string ResortName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    
    /// <summary>
    /// Feature ID'leri JSON array olarak saklanır: "[1,2,3,4]"
    /// </summary>
    public string FeatureIds { get; set; } = "[]";
    
    /// <summary>
    /// Theme ID'leri JSON array olarak saklanır: "[1,2,3]"
    /// </summary>
    public string ThemeIds { get; set; } = "[]";
    
    /// <summary>
    /// Resim URL'leri JSON array olarak saklanır
    /// </summary>
    public string ImageUrls { get; set; } = "[]";
    
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
    
    // Navigation
    public virtual ICollection<SunHotelsRoomCache> Rooms { get; set; } = new List<SunHotelsRoomCache>();
}

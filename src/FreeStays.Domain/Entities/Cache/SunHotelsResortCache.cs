using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Resort/Bölge Cache Entity
/// </summary>
public class SunHotelsResortCache : BaseEntity
{
    public int ResortId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DestinationId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
}

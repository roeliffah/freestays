using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Destinasyon Cache Entity
/// </summary>
public class SunHotelsDestinationCache : BaseEntity
{
    public string DestinationId { get; set; } = string.Empty;
    public string DestinationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryId { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public DateTime LastSyncedAt { get; set; }
}

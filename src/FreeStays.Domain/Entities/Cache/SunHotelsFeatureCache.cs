using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Özellik Cache Entity
/// </summary>
public class SunHotelsFeatureCache : BaseEntity
{
    public int FeatureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
}

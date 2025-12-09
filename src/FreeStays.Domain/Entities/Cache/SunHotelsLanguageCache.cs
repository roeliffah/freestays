using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Dil Cache Entity
/// </summary>
public class SunHotelsLanguageCache : BaseEntity
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime LastSyncedAt { get; set; }
}

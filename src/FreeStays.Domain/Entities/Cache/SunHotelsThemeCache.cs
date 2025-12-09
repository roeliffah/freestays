using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Tema Cache Entity
/// </summary>
public class SunHotelsThemeCache : BaseEntity
{
    public int ThemeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public DateTime LastSyncedAt { get; set; }
}

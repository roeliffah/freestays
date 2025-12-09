using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Yemek Tipi Cache Entity
/// </summary>
public class SunHotelsMealCache : BaseEntity
{
    public int MealId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Labels { get; set; } = string.Empty; // JSON array olarak saklanacak
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
}

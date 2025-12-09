using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Oda Tipi Cache Entity
/// </summary>
public class SunHotelsRoomTypeCache : BaseEntity
{
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
}

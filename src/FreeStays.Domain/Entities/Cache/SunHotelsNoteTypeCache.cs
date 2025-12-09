using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Not Tipi Cache Entity (Hotel ve Room not tipleri için ortak)
/// </summary>
public class SunHotelsNoteTypeCache : BaseEntity
{
    public int NoteTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NoteCategory { get; set; } = string.Empty; // "Hotel" veya "Room"
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
}

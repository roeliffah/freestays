using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities.Cache;

/// <summary>
/// SunHotels Transfer Tipi Cache Entity
/// </summary>
public class SunHotelsTransferTypeCache : BaseEntity
{
    public int TransferTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public DateTime LastSyncedAt { get; set; }
}

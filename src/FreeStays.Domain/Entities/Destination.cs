using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class Destination : BaseEntity
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DestinationType Type { get; set; }
    public bool IsPopular { get; set; }
    public DateTime? SyncedAt { get; set; }
}

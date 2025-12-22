using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class SunhotelsRoomtypesCache
{
    public Guid Id { get; set; }

    public int RoomTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Language { get; set; } = null!;

    public DateTime LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Destination
{
    public Guid Id { get; set; }

    public string ExternalId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Country { get; set; } = null!;

    public int Type { get; set; }

    public bool IsPopular { get; set; }

    public DateTime? SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

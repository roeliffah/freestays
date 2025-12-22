using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class SunhotelsResortsCache
{
    public Guid Id { get; set; }

    public int ResortId { get; set; }

    public string Name { get; set; } = null!;

    public string DestinationId { get; set; } = null!;

    public string DestinationName { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public string Language { get; set; } = null!;

    public DateTime LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string CountryName { get; set; } = null!;
}

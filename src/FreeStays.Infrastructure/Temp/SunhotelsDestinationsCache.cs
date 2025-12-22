using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class SunhotelsDestinationsCache
{
    public Guid Id { get; set; }

    public string DestinationId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public string TimeZone { get; set; } = null!;

    public DateTime LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string CountryId { get; set; } = null!;

    public string DestinationCode { get; set; } = null!;
}

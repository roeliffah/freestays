using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class FeaturedDestination
{
    public Guid Id { get; set; }

    public string DestinationId { get; set; } = null!;

    public string DestinationName { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public string Country { get; set; } = null!;

    public int Priority { get; set; }

    public string Status { get; set; } = null!;

    public string Season { get; set; } = null!;

    public string? Image { get; set; }

    public string? Description { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

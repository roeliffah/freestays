using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class FeaturedDestination : BaseEntity
{
    public string DestinationId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Priority { get; set; } = 999;
    public FeaturedContentStatus Status { get; set; } = FeaturedContentStatus.Active;
    public Season Season { get; set; } = Season.AllSeason;
    public string? Image { get; set; }
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

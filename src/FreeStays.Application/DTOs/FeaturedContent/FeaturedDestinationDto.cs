using FreeStays.Domain.Enums;

namespace FreeStays.Application.DTOs.FeaturedContent;

public class FeaturedDestinationDto
{
    public Guid Id { get; set; }
    public string DestinationId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFeaturedDestinationDto
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

public class UpdateFeaturedDestinationDto
{
    public string? DestinationName { get; set; }
    public string? CountryCode { get; set; }
    public string? Country { get; set; }
    public int? Priority { get; set; }
    public FeaturedContentStatus? Status { get; set; }
    public Season? Season { get; set; }
    public string? Image { get; set; }
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class PublicFeaturedDestinationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int? HotelCount { get; set; }
    public decimal? AveragePrice { get; set; }
    public string? Description { get; set; }
}

using FreeStays.Domain.Enums;

namespace FreeStays.Application.DTOs.FeaturedContent;

public class FeaturedHotelDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public HotelSummaryDto? Hotel { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? CampaignName { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class HotelSummaryDto
{
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string? DestinationName { get; set; }
    public string? Country { get; set; }
    public int? Category { get; set; }
    public decimal? Rating { get; set; }
    public List<string> Images { get; set; } = new();
    public decimal? PriceFrom { get; set; }
    public string? Currency { get; set; }
}

public class FeaturedHotelListDto
{
    public List<FeaturedHotelDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateFeaturedHotelDto
{
    public Guid HotelId { get; set; }
    public int Priority { get; set; } = 999;
    public FeaturedContentStatus Status { get; set; } = FeaturedContentStatus.Active;
    public Season Season { get; set; } = Season.AllSeason;
    public HotelCategory? Category { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? CampaignName { get; set; }
    public decimal? DiscountPercentage { get; set; }
}

public class UpdateFeaturedHotelDto
{
    public int? Priority { get; set; }
    public FeaturedContentStatus? Status { get; set; }
    public Season? Season { get; set; }
    public HotelCategory? Category { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? CampaignName { get; set; }
    public decimal? DiscountPercentage { get; set; }
}

public class BulkPriorityUpdateDto
{
    public List<PriorityItemDto> Items { get; set; } = new();
}

public class PriorityItemDto
{
    public Guid Id { get; set; }
    public int Priority { get; set; }
}

public class PublicFeaturedHotelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? Rating { get; set; }
    public int? Stars { get; set; }
    public decimal? PriceFrom { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? Currency { get; set; }
    public List<string> Images { get; set; } = new();
    public string? Category { get; set; }
    public string? CampaignName { get; set; }
}

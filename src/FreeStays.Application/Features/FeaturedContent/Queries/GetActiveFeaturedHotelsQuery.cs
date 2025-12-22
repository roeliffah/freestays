using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Queries;

public record GetActiveFeaturedHotelsQuery : IRequest<List<PublicFeaturedHotelDto>>
{
    public int Count { get; init; } = 10;
    public Season? Season { get; init; }
    public HotelCategory? Category { get; init; }
}

public class GetActiveFeaturedHotelsQueryHandler : IRequestHandler<GetActiveFeaturedHotelsQuery, List<PublicFeaturedHotelDto>>
{
    private readonly IFeaturedHotelRepository _repository;

    public GetActiveFeaturedHotelsQueryHandler(IFeaturedHotelRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PublicFeaturedHotelDto>> Handle(GetActiveFeaturedHotelsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetActiveAsync(
            request.Count,
            request.Season,
            request.Category,
            cancellationToken);

        return items.Select(f => new PublicFeaturedHotelDto
        {
            Id = f.HotelId,
            Name = f.Hotel?.Name ?? string.Empty,
            City = f.Hotel?.City,
            Country = f.Hotel?.Country,
            Rating = f.Hotel?.Category,
            Stars = f.Hotel?.Category,
            PriceFrom = f.DiscountPercentage.HasValue && f.Hotel?.MinPrice.HasValue == true
                ? f.Hotel.MinPrice * (1 - f.DiscountPercentage.Value / 100)
                : f.Hotel?.MinPrice,
            OriginalPrice = f.Hotel?.MinPrice,
            DiscountPercentage = f.DiscountPercentage,
            Currency = f.Hotel?.Currency,
            Images = new List<string>(), // TODO: Get from Hotel.Images
            Category = f.Category?.ToString(),
            CampaignName = f.CampaignName
        }).ToList();
    }
}

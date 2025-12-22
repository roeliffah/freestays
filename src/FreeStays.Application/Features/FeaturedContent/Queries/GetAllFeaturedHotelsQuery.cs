using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Queries;

public record GetAllFeaturedHotelsQuery : IRequest<FeaturedHotelListDto>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public FeaturedContentStatus? Status { get; init; }
    public Season? Season { get; init; }
    public HotelCategory? Category { get; init; }
}

public class GetAllFeaturedHotelsQueryHandler : IRequestHandler<GetAllFeaturedHotelsQuery, FeaturedHotelListDto>
{
    private readonly IFeaturedHotelRepository _repository;

    public GetAllFeaturedHotelsQueryHandler(IFeaturedHotelRepository repository)
    {
        _repository = repository;
    }

    public async Task<FeaturedHotelListDto> Handle(GetAllFeaturedHotelsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllWithHotelsAsync(
            request.Page,
            request.PageSize,
            request.Status,
            request.Season,
            request.Category,
            cancellationToken);

        var totalCount = await _repository.GetTotalCountAsync(
            request.Status,
            request.Season,
            request.Category,
            cancellationToken);

        return new FeaturedHotelListDto
        {
            Items = items.Select(f => new FeaturedHotelDto
            {
                Id = f.Id,
                HotelId = f.HotelId,
                Hotel = f.Hotel != null ? new HotelSummaryDto
                {
                    HotelId = f.Hotel.Id,
                    HotelName = f.Hotel.Name,
                    DestinationName = f.Hotel.City,
                    Country = f.Hotel.Country,
                    Category = f.Hotel.Category,
                    Rating = f.Hotel.Category,
                    PriceFrom = f.Hotel.MinPrice,
                    Currency = f.Hotel.Currency
                } : null,
                Priority = f.Priority,
                Status = f.Status.ToString(),
                Season = f.Season.ToString(),
                Category = f.Category?.ToString(),
                ValidFrom = f.ValidFrom,
                ValidUntil = f.ValidUntil,
                CampaignName = f.CampaignName,
                DiscountPercentage = f.DiscountPercentage,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Queries;

public record GetActiveFeaturedDestinationsQuery : IRequest<List<PublicFeaturedDestinationDto>>
{
    public int Count { get; init; } = 10;
    public Season? Season { get; init; }
}

public class GetActiveFeaturedDestinationsQueryHandler : IRequestHandler<GetActiveFeaturedDestinationsQuery, List<PublicFeaturedDestinationDto>>
{
    private readonly IFeaturedDestinationRepository _repository;

    public GetActiveFeaturedDestinationsQueryHandler(IFeaturedDestinationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PublicFeaturedDestinationDto>> Handle(GetActiveFeaturedDestinationsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetActiveAsync(request.Count, request.Season, cancellationToken);

        return items.Select(d => new PublicFeaturedDestinationDto
        {
            Id = d.DestinationId,
            Name = d.DestinationName,
            Country = d.Country,
            CountryCode = d.CountryCode,
            Image = d.Image,
            Description = d.Description,
            // TODO: Get hotel count and average price from database
            HotelCount = null,
            AveragePrice = null
        }).ToList();
    }
}

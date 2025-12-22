using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Queries;

public record GetAllFeaturedDestinationsQuery : IRequest<List<FeaturedDestinationDto>>
{
    public FeaturedContentStatus? Status { get; init; }
    public Season? Season { get; init; }
}

public class GetAllFeaturedDestinationsQueryHandler : IRequestHandler<GetAllFeaturedDestinationsQuery, List<FeaturedDestinationDto>>
{
    private readonly IFeaturedDestinationRepository _repository;

    public GetAllFeaturedDestinationsQueryHandler(IFeaturedDestinationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<FeaturedDestinationDto>> Handle(GetAllFeaturedDestinationsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(request.Status, request.Season, cancellationToken);

        return items.Select(d => new FeaturedDestinationDto
        {
            Id = d.Id,
            DestinationId = d.DestinationId,
            DestinationName = d.DestinationName,
            CountryCode = d.CountryCode,
            Country = d.Country,
            Priority = d.Priority,
            Status = d.Status.ToString(),
            Season = d.Season.ToString(),
            Image = d.Image,
            Description = d.Description,
            ValidFrom = d.ValidFrom,
            ValidUntil = d.ValidUntil,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }
}

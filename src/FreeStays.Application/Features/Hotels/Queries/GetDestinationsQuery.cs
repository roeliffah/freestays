using AutoMapper;
using FreeStays.Application.DTOs.Hotels;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Hotels.Queries;

public record GetDestinationsQuery(string? Query = null) : IRequest<List<DestinationDto>>;

public class GetDestinationsQueryHandler : IRequestHandler<GetDestinationsQuery, List<DestinationDto>>
{
    private readonly IDestinationRepository _destinationRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    
    public GetDestinationsQueryHandler(
        IDestinationRepository destinationRepository,
        ICacheService cacheService,
        IMapper mapper)
    {
        _destinationRepository = destinationRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }
    
    public async Task<List<DestinationDto>> Handle(GetDestinationsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            var cacheKey = "destinations:popular";
            
            var cached = await _cacheService.GetAsync<List<DestinationDto>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
            
            var popular = await _destinationRepository.GetPopularAsync(20, cancellationToken);
            var dtos = _mapper.Map<List<DestinationDto>>(popular);
            
            await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(24), cancellationToken);
            
            return dtos;
        }
        
        var destinations = await _destinationRepository.SearchAsync(request.Query, cancellationToken);
        return _mapper.Map<List<DestinationDto>>(destinations);
    }
}

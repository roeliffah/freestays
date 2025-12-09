using AutoMapper;
using FreeStays.Application.DTOs.Hotels;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Hotels.Queries;

public record GetFeaturedHotelsQuery(int Count = 10) : IRequest<List<HotelSearchResultDto>>;

public class GetFeaturedHotelsQueryHandler : IRequestHandler<GetFeaturedHotelsQuery, List<HotelSearchResultDto>>
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    
    public GetFeaturedHotelsQueryHandler(
        IHotelRepository hotelRepository,
        ICacheService cacheService,
        IMapper mapper)
    {
        _hotelRepository = hotelRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }
    
    public async Task<List<HotelSearchResultDto>> Handle(GetFeaturedHotelsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"hotels:featured:{request.Count}";
        
        var cached = await _cacheService.GetAsync<List<HotelSearchResultDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;
        
        var hotels = await _hotelRepository.GetFeaturedAsync(request.Count, cancellationToken);
        var dtos = _mapper.Map<List<HotelSearchResultDto>>(hotels);
        
        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromHours(1), cancellationToken);
        
        return dtos;
    }
}

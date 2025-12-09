using AutoMapper;
using FreeStays.Application.DTOs.Hotels;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Hotels.Queries;

public record SearchHotelsQuery : IRequest<List<HotelSearchResultDto>>
{
    public string? Destination { get; init; }
    public DateTime? CheckIn { get; init; }
    public DateTime? CheckOut { get; init; }
    public int? Guests { get; init; }
    public int? MinCategory { get; init; }
    public decimal? MaxPrice { get; init; }
}

public class SearchHotelsQueryHandler : IRequestHandler<SearchHotelsQuery, List<HotelSearchResultDto>>
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    
    public SearchHotelsQueryHandler(
        IHotelRepository hotelRepository,
        ICacheService cacheService,
        IMapper mapper)
    {
        _hotelRepository = hotelRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }
    
    public async Task<List<HotelSearchResultDto>> Handle(SearchHotelsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"hotels:search:{request.Destination}:{request.MinCategory}:{request.MaxPrice}";
        
        var cached = await _cacheService.GetAsync<List<HotelSearchResultDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;
        
        var hotels = await _hotelRepository.SearchAsync(
            request.Destination,
            null,
            request.MinCategory,
            request.MaxPrice,
            cancellationToken);
        
        var dtos = _mapper.Map<List<HotelSearchResultDto>>(hotels);
        
        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), cancellationToken);
        
        return dtos;
    }
}

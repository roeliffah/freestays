using AutoMapper;
using FreeStays.Application.DTOs.Hotels;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Hotels.Queries;

public record GetHotelByIdQuery(Guid Id) : IRequest<HotelDto?>;

public class GetHotelByIdQueryHandler : IRequestHandler<GetHotelByIdQuery, HotelDto?>
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    
    public GetHotelByIdQueryHandler(
        IHotelRepository hotelRepository,
        ICacheService cacheService,
        IMapper mapper)
    {
        _hotelRepository = hotelRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }
    
    public async Task<HotelDto?> Handle(GetHotelByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"hotel:{request.Id}";
        
        var cached = await _cacheService.GetAsync<HotelDto>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;
        
        var hotel = await _hotelRepository.GetWithDetailsAsync(request.Id, cancellationToken);
        if (hotel == null)
            return null;
        
        var dto = _mapper.Map<HotelDto>(hotel);
        
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromHours(1), cancellationToken);
        
        return dto;
    }
}

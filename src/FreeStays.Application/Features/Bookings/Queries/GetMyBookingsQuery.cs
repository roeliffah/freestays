using AutoMapper;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.Bookings;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Bookings.Queries;

public record GetMyBookingsQuery : IRequest<List<BookingDto>>;

public class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, List<BookingDto>>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    
    public GetMyBookingsQueryHandler(
        IBookingRepository bookingRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }
    
    public async Task<List<BookingDto>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            throw new UnauthorizedException();
        
        var bookings = await _bookingRepository.GetByUserIdAsync(
            _currentUserService.UserId.Value, 
            cancellationToken);
        
        return _mapper.Map<List<BookingDto>>(bookings);
    }
}

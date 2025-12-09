using AutoMapper;
using FreeStays.Application.DTOs.Bookings;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Bookings.Queries;

public record GetBookingByIdQuery(Guid Id) : IRequest<BookingDto>;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IMapper _mapper;
    
    public GetBookingByIdQueryHandler(
        IBookingRepository bookingRepository,
        IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
    }
    
    public async Task<BookingDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetWithDetailsAsync(request.Id, cancellationToken);
        
        if (booking == null)
            throw new NotFoundException("Booking", request.Id);
        
        return _mapper.Map<BookingDto>(booking);
    }
}

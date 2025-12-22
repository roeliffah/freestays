using AutoMapper;
using FreeStays.Application.DTOs.Bookings;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Admin.Queries;

public record GetAllBookingsQuery : IRequest<BookingListDto>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public BookingStatus? Status { get; init; }
    public BookingType? Type { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public record BookingListDto
{
    public List<BookingDto> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

public class GetAllBookingsQueryHandler : IRequestHandler<GetAllBookingsQuery, BookingListDto>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IMapper _mapper;

    public GetAllBookingsQueryHandler(IBookingRepository bookingRepository, IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
    }

    public async Task<BookingListDto> Handle(GetAllBookingsQuery request, CancellationToken cancellationToken)
    {
        // Get all bookings based on filters
        var allBookings = new List<Booking>();

        if (request.Status.HasValue)
        {
            allBookings = (await _bookingRepository.GetByStatusAsync(request.Status.Value, cancellationToken))
                .ToList();
        }
        else
        {
            // Get all bookings
            allBookings = (await _bookingRepository.GetAllAsync(cancellationToken))
                .ToList();
        }

        // Apply type filter
        if (request.Type.HasValue)
        {
            allBookings = allBookings.Where(b => b.Type == request.Type).ToList();
        }

        // Apply date filters
        if (request.FromDate.HasValue)
        {
            allBookings = allBookings.Where(b => b.CreatedAt >= request.FromDate).ToList();
        }

        if (request.ToDate.HasValue)
        {
            allBookings = allBookings.Where(b => b.CreatedAt <= request.ToDate).ToList();
        }

        // Get total count before pagination
        var totalCount = allBookings.Count;

        // Apply pagination
        var paginatedBookings = allBookings
            .OrderByDescending(b => b.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BookingListDto
        {
            Items = _mapper.Map<List<BookingDto>>(paginatedBookings),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

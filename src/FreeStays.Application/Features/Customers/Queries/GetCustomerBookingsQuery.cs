using FreeStays.Application.DTOs.Customers;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Customers.Queries;

public record GetCustomerBookingsQuery(Guid CustomerId) : IRequest<List<CustomerBookingDto>>;

public class GetCustomerBookingsQueryHandler : IRequestHandler<GetCustomerBookingsQuery, List<CustomerBookingDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IBookingRepository _bookingRepository;

    public GetCustomerBookingsQueryHandler(
        ICustomerRepository customerRepository,
        IBookingRepository bookingRepository)
    {
        _customerRepository = customerRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<List<CustomerBookingDto>> Handle(GetCustomerBookingsQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", request.CustomerId);
        }

        var bookings = await _bookingRepository.GetByUserIdAsync(customer.UserId, cancellationToken);

        return bookings.Select(b => new CustomerBookingDto
        {
            Id = b.Id,
            HotelName = b.HotelBooking?.Hotel?.Name ?? "Unknown",
            CheckIn = b.HotelBooking?.CheckIn ?? DateTime.MinValue,
            CheckOut = b.HotelBooking?.CheckOut ?? DateTime.MinValue,
            TotalPrice = b.TotalPrice,
            Status = b.Status.ToString(),
            CreatedAt = b.CreatedAt
        }).ToList();
    }
}

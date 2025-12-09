using FreeStays.Application.DTOs.Customers;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", request.Id);
        }

        return new CustomerDto
        {
            Id = customer.Id,
            UserId = customer.UserId,
            Email = customer.User?.Email ?? string.Empty,
            Name = customer.User?.Name ?? string.Empty,
            Phone = customer.User?.Phone,
            TotalBookings = customer.TotalBookings,
            TotalSpent = customer.TotalSpent,
            LastBookingAt = customer.LastBookingAt,
            Notes = customer.Notes,
            IsBlocked = customer.IsBlocked,
            CreatedAt = customer.CreatedAt
        };
    }
}

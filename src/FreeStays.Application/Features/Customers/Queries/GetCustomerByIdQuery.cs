using FreeStays.Application.DTOs.Customers;
using FreeStays.Application.Features.Customers.Extensions;
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

        return customer.ToDto();
    }
}

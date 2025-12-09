using FreeStays.Application.DTOs.Customers;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Customers.Commands;

public record UpdateCustomerCommand : IRequest<CustomerDto>
{
    public Guid Id { get; init; }
    public string? Notes { get; init; }
    public bool IsBlocked { get; init; }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", request.Id);
        }

        customer.Notes = request.Notes;
        customer.IsBlocked = request.IsBlocked;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

using FreeStays.Application.DTOs.Customers;
using FreeStays.Application.Features.Customers.Extensions;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Customers.Commands;

public record UpdateCustomerCommand : IRequest<CustomerDto>
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Phone { get; init; }
    public UserRole? Role { get; init; }
    public string? Notes { get; init; }
    public bool? IsBlocked { get; init; }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", request.Id);
        }

        var user = await _userRepository.GetByIdAsync(customer.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", customer.UserId);
        }

        // User bilgilerini güncelle
        if (request.Name != null)
            user.Name = request.Name;

        if (request.Phone != null)
            user.Phone = request.Phone;

        if (request.Role.HasValue)
            user.Role = request.Role.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Customer bilgilerini güncelle
        if (request.Notes != null)
            customer.Notes = request.Notes;

        if (request.IsBlocked.HasValue)
            customer.IsBlocked = request.IsBlocked.Value;

        customer.UpdatedAt = DateTime.UtcNow;
        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // User navigation property'sini set et
        customer.User = user;
        return customer.ToDto();
    }
}

using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Customers.Commands;

public record DeleteCustomerCommand(Guid Id) : IRequest<Unit>;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Unit>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", request.Id);
        }

        // İlişkili User'ı da sil
        var user = await _userRepository.GetByIdAsync(customer.UserId, cancellationToken);
        if (user != null)
        {
            await _userRepository.DeleteAsync(user, cancellationToken);
        }

        await _customerRepository.DeleteAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

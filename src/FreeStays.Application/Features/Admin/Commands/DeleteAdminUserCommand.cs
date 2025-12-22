using FreeStays.Domain.Enums;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;
using DomainValidationException = FreeStays.Domain.Exceptions.ValidationException;

namespace FreeStays.Application.Features.Admin.Commands;

public record DeleteAdminUserCommand(Guid Id) : IRequest<Unit>;

public class DeleteAdminUserCommandHandler : IRequestHandler<DeleteAdminUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAdminUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", request.Id.ToString());
        }

        // Customer rollü kullanıcıları silmeyi engelle
        if (user.Role == UserRole.Customer)
        {
            throw new DomainValidationException("User", "Customer kullanıcıları bu endpoint ile silinemez.");
        }

        // Soft delete - IsActive = false
        user.IsActive = false;
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

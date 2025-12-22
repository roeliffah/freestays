using FluentValidation;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.Admin;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;
using DomainValidationException = FreeStays.Domain.Exceptions.ValidationException;

namespace FreeStays.Application.Features.Admin.Commands;

public record UpdateAdminUserCommand : IRequest<AdminUserDto>
{
    public Guid Id { get; init; }
    public string? Name { get; init; }  // Opsiyonel - gelmezse mevcut değer kullanılır
    public string? Phone { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
    public string? NewPassword { get; init; }
}

public class UpdateAdminUserCommandValidator : AbstractValidator<UpdateAdminUserCommand>
{
    public UpdateAdminUserCommandValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("İsim 200 karakteri geçemez.");
        });

        When(x => x.Role.HasValue, () =>
        {
            RuleFor(x => x.Role!.Value)
                .Must(r => r == UserRole.Admin || r == UserRole.SuperAdmin)
                .WithMessage("Sadece Admin veya SuperAdmin rolü atanabilir.");
        });

        When(x => !string.IsNullOrEmpty(x.NewPassword), () =>
        {
            RuleFor(x => x.NewPassword)
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");
        });
    }
}

public class UpdateAdminUserCommandHandler : IRequestHandler<UpdateAdminUserCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateAdminUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<AdminUserDto> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", request.Id.ToString());
        }

        // Customer rollü kullanıcıları düzenlemeyi engelle
        if (user.Role == UserRole.Customer)
        {
            throw new DomainValidationException("User", "Customer kullanıcıları bu endpoint ile düzenlenemez.");
        }

        // Sadece değişen alanları güncelle
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            user.Name = request.Name;
        }

        if (request.Phone != null)  // null kontrolü - boş string bile set edilebilir
        {
            user.Phone = request.Phone;
        }

        if (request.Role.HasValue)
        {
            user.Role = request.Role.Value;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Phone = user.Phone,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}

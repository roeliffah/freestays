using FluentValidation;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.Admin;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;
using DomainValidationException = FreeStays.Domain.Exceptions.ValidationException;

namespace FreeStays.Application.Features.Admin.Commands;

public record CreateAdminUserCommand : IRequest<AdminUserDto>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public UserRole Role { get; init; } = UserRole.Admin;
}

public class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand>
{
    public CreateAdminUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email gereklidir.")
            .EmailAddress().WithMessage("Geçersiz email formatı.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İsim gereklidir.")
            .MaximumLength(200).WithMessage("İsim 200 karakteri geçemez.");

        RuleFor(x => x.Role)
            .Must(r => r == UserRole.Admin || r == UserRole.SuperAdmin)
            .WithMessage("Sadece Admin veya SuperAdmin rolü atanabilir.");
    }
}

public class CreateAdminUserCommandHandler : IRequestHandler<CreateAdminUserCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateAdminUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<AdminUserDto> Handle(CreateAdminUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new DomainValidationException("Email", "Email zaten kullanılıyor.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Name = request.Name,
            Phone = request.Phone,
            Role = request.Role,
            IsActive = true,
            Locale = "tr"
        };

        await _userRepository.AddAsync(user, cancellationToken);
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

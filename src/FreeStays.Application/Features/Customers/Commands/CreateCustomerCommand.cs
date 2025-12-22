using FluentValidation;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.Customers;
using FreeStays.Application.Features.Customers.Extensions;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;
using DomainValidationException = FreeStays.Domain.Exceptions.ValidationException;

namespace FreeStays.Application.Features.Customers.Commands;

public record CreateCustomerCommand : IRequest<CustomerDto>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public UserRole Role { get; init; } = UserRole.Customer;
    public string? Notes { get; init; }
}

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
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
    }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateCustomerCommandHandler(
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
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

        // Customer kaydı oluştur (sadece Customer rollü kullanıcılar için)
        if (user.Role == UserRole.Customer)
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TotalBookings = 0,
                TotalSpent = 0,
                IsBlocked = false,
                Notes = request.Notes
            };

            await _customerRepository.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // User navigation property'sini set et
            customer.User = user;
            return customer.ToDto();
        }
        else
        {
            // Admin/SuperAdmin rollü kullanıcılar için sadece User kaydı
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Admin için boş CustomerDto döndür veya farklı bir response tipi kullan
            return new CustomerDto
            {
                Id = Guid.Empty,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Role = user.Role,
                TotalBookings = 0,
                TotalSpent = 0,
                IsBlocked = false
            };
        }
    }
}

using FreeStays.Application.DTOs.Admin;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Admin.Queries;

public record GetAdminUserByIdQuery(Guid Id) : IRequest<AdminUserDto>;

public class GetAdminUserByIdQueryHandler : IRequestHandler<GetAdminUserByIdQuery, AdminUserDto>
{
    private readonly IUserRepository _userRepository;

    public GetAdminUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AdminUserDto> Handle(GetAdminUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", request.Id.ToString());
        }

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

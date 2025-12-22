using FreeStays.Application.DTOs.Admin;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Admin.Queries;

public record GetAdminUsersQuery : IRequest<AdminUsersListDto>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}

public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, AdminUsersListDto>
{
    private readonly IUserRepository _userRepository;

    public GetAdminUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AdminUsersListDto> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _userRepository.GetAdminUsersPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            cancellationToken);

        var adminUsers = items.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Email = u.Email,
            Name = u.Name,
            Phone = u.Phone,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();

        return new AdminUsersListDto
        {
            Items = adminUsers,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}

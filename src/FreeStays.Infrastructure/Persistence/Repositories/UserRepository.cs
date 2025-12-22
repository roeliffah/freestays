using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAdminUsersPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(u => u.Role == Domain.Enums.UserRole.Admin || u.Role == Domain.Enums.UserRole.SuperAdmin)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(search) ||
                u.Name.ToLower().Contains(search) ||
                (u.Phone != null && u.Phone.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

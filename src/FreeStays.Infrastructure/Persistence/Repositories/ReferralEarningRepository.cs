using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class ReferralEarningRepository : Repository<ReferralEarning>, IReferralEarningRepository
{
    public ReferralEarningRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ReferralEarning>> GetByReferrerUserIdAsync(Guid referrerUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ReferrerUserId == referrerUserId)
            .Include(r => r.ReferredUser)
            .Include(r => r.Booking)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReferralEarning>> GetByReferredUserIdAsync(Guid referredUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ReferredUserId == referredUserId)
            .Include(r => r.ReferrerUser)
            .Include(r => r.Booking)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalEarningsAsync(Guid referrerUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ReferrerUserId == referrerUserId && r.Status == ReferralEarningStatus.Paid)
            .SumAsync(r => r.Amount, cancellationToken);
    }

    public async Task<(IReadOnlyList<ReferralEarning> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? referrerUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.ReferrerUser)
            .Include(r => r.ReferredUser)
            .Include(r => r.Booking)
            .AsQueryable();

        if (referrerUserId.HasValue)
        {
            query = query.Where(r => r.ReferrerUserId == referrerUserId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

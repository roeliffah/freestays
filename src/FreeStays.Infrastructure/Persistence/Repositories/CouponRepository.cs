using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    public CouponRepository(FreeStaysDbContext context) : base(context)
    {
    }
    
    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower(), cancellationToken);
    }
    
    public async Task<IReadOnlyList<Coupon>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(c => c.IsActive && c.ValidFrom <= now && c.ValidUntil >= now)
            .Where(c => !c.MaxUses.HasValue || c.UsedCount < c.MaxUses)
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }
}

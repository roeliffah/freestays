using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class FeaturedHotelRepository : Repository<FeaturedHotel>, IFeaturedHotelRepository
{
    public FeaturedHotelRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<FeaturedHotel?> GetByIdWithHotelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Hotel)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FeaturedHotel>> GetAllWithHotelsAsync(
        int? page = null,
        int? pageSize = null,
        FeaturedContentStatus? status = null,
        Season? season = null,
        HotelCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(f => f.Hotel).AsQueryable();

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (season.HasValue)
            query = query.Where(f => f.Season == season.Value);

        if (category.HasValue)
            query = query.Where(f => f.Category == category.Value);

        query = query.OrderBy(f => f.Priority).ThenByDescending(f => f.CreatedAt);

        if (page.HasValue && pageSize.HasValue)
        {
            query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        FeaturedContentStatus? status = null,
        Season? season = null,
        HotelCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (season.HasValue)
            query = query.Where(f => f.Season == season.Value);

        if (category.HasValue)
            query = query.Where(f => f.Category == category.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeaturedHotel>> GetActiveAsync(
        int count = 10,
        Season? season = null,
        HotelCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        var query = _dbSet
            .Include(f => f.Hotel)
            .Where(f => f.Status == FeaturedContentStatus.Active ||
                       (f.Status == FeaturedContentStatus.Scheduled &&
                        f.ValidFrom <= now && f.ValidUntil >= now));

        if (season.HasValue)
            query = query.Where(f => f.Season == season.Value || f.Season == Season.AllSeason);

        if (category.HasValue)
            query = query.Where(f => f.Category == category.Value);

        return await query
            .OrderBy(f => f.Priority)
            .ThenByDescending(f => f.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveByHotelIdAsync(Guid hotelId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        var query = _dbSet.Where(f => f.HotelId == hotelId &&
                                     (f.Status == FeaturedContentStatus.Active ||
                                      (f.Status == FeaturedContentStatus.Scheduled &&
                                       f.ValidFrom <= now && f.ValidUntil >= now)));

        if (excludeId.HasValue)
            query = query.Where(f => f.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task UpdatePrioritiesAsync(Dictionary<Guid, int> priorities, CancellationToken cancellationToken = default)
    {
        foreach (var (id, priority) in priorities)
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                entity.Priority = priority;
            }
        }
    }
}

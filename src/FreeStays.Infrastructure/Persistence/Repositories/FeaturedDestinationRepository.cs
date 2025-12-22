using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class FeaturedDestinationRepository : Repository<FeaturedDestination>, IFeaturedDestinationRepository
{
    public FeaturedDestinationRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<FeaturedDestination>> GetAllAsync(
        FeaturedContentStatus? status = null,
        Season? season = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (season.HasValue)
            query = query.Where(f => f.Season == season.Value);

        return await query
            .OrderBy(f => f.Priority)
            .ThenByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeaturedDestination>> GetActiveAsync(
        int count = 10,
        Season? season = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        var query = _dbSet.Where(f => f.Status == FeaturedContentStatus.Active ||
                                     (f.Status == FeaturedContentStatus.Scheduled &&
                                      f.ValidFrom <= now && f.ValidUntil >= now));

        if (season.HasValue)
            query = query.Where(f => f.Season == season.Value || f.Season == Season.AllSeason);

        return await query
            .OrderBy(f => f.Priority)
            .ThenByDescending(f => f.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveByDestinationIdAsync(string destinationId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        var query = _dbSet.Where(f => f.DestinationId == destinationId &&
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

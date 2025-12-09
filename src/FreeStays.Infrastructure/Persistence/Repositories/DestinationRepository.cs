using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class DestinationRepository : Repository<Destination>, IDestinationRepository
{
    public DestinationRepository(FreeStaysDbContext context) : base(context)
    {
    }
    
    public async Task<Destination?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.ExternalId == externalId, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Destination>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var loweredQuery = query.ToLower();
        return await _dbSet
            .Where(d => d.Name.ToLower().Contains(loweredQuery) || d.Country.ToLower().Contains(loweredQuery))
            .OrderBy(d => d.Name)
            .Take(20)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Destination>> GetPopularAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.IsPopular)
            .OrderBy(d => d.Name)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

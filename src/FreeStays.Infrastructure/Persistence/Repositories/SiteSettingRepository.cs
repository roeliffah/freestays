using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class SiteSettingRepository : Repository<SiteSetting>, ISiteSettingRepository
{
    public SiteSettingRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<SiteSetting>> GetByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Group == group)
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbSet.ToListAsync(cancellationToken);
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }
}

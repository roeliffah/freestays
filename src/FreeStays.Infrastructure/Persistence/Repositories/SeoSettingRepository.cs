using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class SeoSettingRepository : Repository<SeoSetting>, ISeoSettingRepository
{
    public SeoSettingRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<SeoSetting?> GetByLocaleAndPageTypeAsync(string locale, string pageType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Locale == locale && s.PageType == pageType, cancellationToken);
    }

    public async Task<IReadOnlyList<SeoSetting>> GetByLocaleAsync(string locale, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Locale == locale)
            .OrderBy(s => s.PageType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAllLocalesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Select(s => s.Locale)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync(cancellationToken);
    }
}

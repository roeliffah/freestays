using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class StaticPageRepository : Repository<StaticPage>, IStaticPageRepository
{
    public StaticPageRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<StaticPage?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<StaticPage?> GetBySlugWithTranslationsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<StaticPageTranslation?> GetTranslationAsync(Guid pageId, string locale, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StaticPageTranslation>()
            .FirstOrDefaultAsync(t => t.PageId == pageId && t.Locale == locale, cancellationToken);
    }

    public async Task<IReadOnlyList<StaticPage>> GetAllWithTranslationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Translations)
            .OrderBy(p => p.Slug)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StaticPage>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Translations)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Slug)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(p => p.Slug == slug);
        
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

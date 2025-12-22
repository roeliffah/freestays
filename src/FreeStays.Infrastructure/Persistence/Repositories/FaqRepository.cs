using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class FaqRepository : Repository<Faq>, IFaqRepository
{
    public FaqRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<Faq?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Translations)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Faq>> GetAllWithTranslationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Translations)
            .OrderBy(f => f.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Faq>> GetActiveWithTranslationsAsync(string? locale = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(f => f.Translations)
            .Where(f => f.IsActive);

        if (!string.IsNullOrEmpty(locale))
        {
            query = query.Where(f => f.Translations.Any(t => t.Locale == locale));
        }

        return await query
            .OrderBy(f => f.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Faq>> GetByCategoryAsync(string category, string? locale = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(f => f.Translations)
            .Where(f => f.IsActive && f.Category == category);

        if (!string.IsNullOrEmpty(locale))
        {
            query = query.Where(f => f.Translations.Any(t => t.Locale == locale));
        }

        return await query
            .OrderBy(f => f.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteTranslationsAsync(Guid faqId, CancellationToken cancellationToken = default)
    {
        var translations = await _context.Set<FaqTranslation>()
            .Where(t => t.FaqId == faqId)
            .ToListAsync(cancellationToken);
        
        _context.Set<FaqTranslation>().RemoveRange(translations);
    }

    public async Task AddTranslationAsync(FaqTranslation translation, CancellationToken cancellationToken = default)
    {
        await _context.Set<FaqTranslation>().AddAsync(translation, cancellationToken);
    }
}

using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class TranslationRepository : Repository<Translation>, ITranslationRepository
{
    public TranslationRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Translation>> GetByLocaleAsync(string locale, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.Locale == locale)
            .OrderBy(t => t.Namespace)
            .ThenBy(t => t.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Translation>> GetByNamespaceAsync(string locale, string @namespace, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.Locale == locale && t.Namespace == @namespace)
            .OrderBy(t => t.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<Translation?> GetByKeyAsync(string locale, string @namespace, string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Locale == locale && t.Namespace == @namespace && t.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAllLocalesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Select(t => t.Locale)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAllNamespacesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Select(t => t.Namespace)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
    }
}

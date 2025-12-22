using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IStaticPageRepository : IRepository<StaticPage>
{
    Task<StaticPage?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StaticPage?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<StaticPage?> GetBySlugWithTranslationsAsync(string slug, CancellationToken cancellationToken = default);
    Task<StaticPageTranslation?> GetTranslationAsync(Guid pageId, string locale, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaticPage>> GetAllWithTranslationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaticPage>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task DeleteTranslationsAsync(Guid pageId, CancellationToken cancellationToken = default);
    Task AddTranslationAsync(StaticPageTranslation translation, CancellationToken cancellationToken = default);
}

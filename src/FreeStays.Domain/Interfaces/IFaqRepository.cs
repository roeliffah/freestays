using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IFaqRepository : IRepository<Faq>
{
    Task<Faq?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Faq>> GetAllWithTranslationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Faq>> GetActiveWithTranslationsAsync(string? locale = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Faq>> GetByCategoryAsync(string category, string? locale = null, CancellationToken cancellationToken = default);
    Task DeleteTranslationsAsync(Guid faqId, CancellationToken cancellationToken = default);
    Task AddTranslationAsync(FaqTranslation translation, CancellationToken cancellationToken = default);
}

using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface ITranslationRepository : IRepository<Translation>
{
    Task<IReadOnlyList<Translation>> GetByLocaleAsync(string locale, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Translation>> GetByNamespaceAsync(string locale, string @namespace, CancellationToken cancellationToken = default);
    Task<Translation?> GetByKeyAsync(string locale, string @namespace, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllLocalesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllNamespacesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Translation key'den değer getir (namespace dahil key formatında: "namespace.key")
    /// </summary>
    Task<string> GetTranslationAsync(string fullKey, string locale, CancellationToken cancellationToken = default);
}

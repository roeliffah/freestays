using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface ISiteSettingRepository : IRepository<SiteSetting>
{
    Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteSetting>> GetByGroupAsync(string group, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default);
}

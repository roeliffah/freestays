using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface ISeoSettingRepository : IRepository<SeoSetting>
{
    Task<SeoSetting?> GetByLocaleAndPageTypeAsync(string locale, string pageType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SeoSetting>> GetByLocaleAsync(string locale, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllLocalesAsync(CancellationToken cancellationToken = default);
}

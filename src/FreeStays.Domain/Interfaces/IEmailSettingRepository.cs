namespace FreeStays.Domain.Interfaces;

public interface IEmailSettingRepository : IRepository<Domain.Entities.EmailSetting>
{
    Task<Domain.Entities.EmailSetting?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<Domain.Entities.EmailSetting?> GetActiveAsync(CancellationToken cancellationToken = default);
}

using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

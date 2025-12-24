using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IExternalServiceConfigRepository : IRepository<ExternalServiceConfig>
{
    Task<ExternalServiceConfig?> GetByServiceNameAsync(string serviceName, CancellationToken cancellationToken = default);
}

using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IDestinationRepository : IRepository<Destination>
{
    Task<Destination?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Destination>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Destination>> GetPopularAsync(int count, CancellationToken cancellationToken = default);
}

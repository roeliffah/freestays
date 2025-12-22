using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Interfaces;

public interface IFeaturedDestinationRepository : IRepository<FeaturedDestination>
{
    Task<IReadOnlyList<FeaturedDestination>> GetAllAsync(
        FeaturedContentStatus? status = null,
        Season? season = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeaturedDestination>> GetActiveAsync(
        int count = 10,
        Season? season = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveByDestinationIdAsync(string destinationId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task UpdatePrioritiesAsync(Dictionary<Guid, int> priorities, CancellationToken cancellationToken = default);
}

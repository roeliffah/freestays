using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IHotelRepository : IRepository<Hotel>
{
    Task<Hotel?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Hotel>> SearchAsync(string? city, string? country, int? minCategory, decimal? maxPrice, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Hotel>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
    Task<Hotel?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}

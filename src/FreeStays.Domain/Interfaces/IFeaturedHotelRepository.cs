using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Interfaces;

public interface IFeaturedHotelRepository : IRepository<FeaturedHotel>
{
    Task<FeaturedHotel?> GetByIdWithHotelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeaturedHotel>> GetAllWithHotelsAsync(
        int? page = null,
        int? pageSize = null,
        FeaturedContentStatus? status = null,
        Season? season = null,
        HotelCategory? category = null,
        CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(
        FeaturedContentStatus? status = null,
        Season? season = null,
        HotelCategory? category = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeaturedHotel>> GetActiveAsync(
        int count = 10,
        Season? season = null,
        HotelCategory? category = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveByHotelIdAsync(Guid hotelId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task UpdatePrioritiesAsync(Dictionary<Guid, int> priorities, CancellationToken cancellationToken = default);
}

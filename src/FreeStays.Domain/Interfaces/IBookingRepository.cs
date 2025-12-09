using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Interfaces;

public interface IBookingRepository : IRepository<Booking>
{
    Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Booking?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetRecentBookingsAsync(int count, CancellationToken cancellationToken = default);
}

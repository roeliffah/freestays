using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IReferralEarningRepository : IRepository<ReferralEarning>
{
    Task<IReadOnlyList<ReferralEarning>> GetByReferrerUserIdAsync(Guid referrerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferralEarning>> GetByReferredUserIdAsync(Guid referredUserId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalEarningsAsync(Guid referrerUserId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ReferralEarning> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? referrerUserId = null,
        CancellationToken cancellationToken = default);
}

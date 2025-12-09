using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface ICouponRepository : IRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Coupon>> GetActiveAsync(CancellationToken cancellationToken = default);
}

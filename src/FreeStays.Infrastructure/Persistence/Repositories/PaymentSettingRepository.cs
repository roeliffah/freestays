using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class PaymentSettingRepository : Repository<PaymentSetting>, IPaymentSettingRepository
{
    public PaymentSettingRepository(FreeStaysDbContext context) : base(context)
    {
    }

    public async Task<PaymentSetting?> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Provider == provider, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentSetting>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.Provider)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentSetting?> GetActiveLiveProviderAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.IsActive && p.IsLive, cancellationToken);
    }
}

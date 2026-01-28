using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface IPaymentSettingRepository : IRepository<PaymentSetting>
{
    Task<PaymentSetting?> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentSetting>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<PaymentSetting?> GetActiveSingleAsync(CancellationToken cancellationToken = default);
    Task<PaymentSetting?> GetActiveLiveProviderAsync(CancellationToken cancellationToken = default);
}

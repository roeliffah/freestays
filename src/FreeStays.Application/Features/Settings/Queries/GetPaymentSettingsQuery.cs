using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Queries;

public record GetPaymentSettingsQuery : IRequest<PaymentSettingDto?>;

public class GetPaymentSettingsQueryHandler : IRequestHandler<GetPaymentSettingsQuery, PaymentSettingDto?>
{
    private readonly IPaymentSettingRepository _paymentSettingRepository;

    public GetPaymentSettingsQueryHandler(IPaymentSettingRepository paymentSettingRepository)
    {
        _paymentSettingRepository = paymentSettingRepository;
    }

    public async Task<PaymentSettingDto?> Handle(GetPaymentSettingsQuery request, CancellationToken cancellationToken)
    {
        // Sadece stripe kullanıldığı için stripe ayarını getir
        var setting = await _paymentSettingRepository.GetByProviderAsync("stripe", cancellationToken);

        if (setting == null)
        {
            return null;
        }

        return new PaymentSettingDto
        {
            Id = setting.Id,
            Provider = setting.Provider,
            TestModePublicKey = setting.TestModePublicKey,
            TestModeSecretKey = setting.TestModeSecretKey,
            LiveModePublicKey = setting.LiveModePublicKey,
            LiveModeSecretKey = setting.LiveModeSecretKey,
            WebhookSecret = setting.WebhookSecret,
            IsLive = setting.IsLive,
            IsActive = setting.IsActive
        };
    }
}

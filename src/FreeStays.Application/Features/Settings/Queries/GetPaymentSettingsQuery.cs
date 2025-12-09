using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Queries;

public record GetPaymentSettingsQuery : IRequest<List<PaymentSettingDto>>;

public class GetPaymentSettingsQueryHandler : IRequestHandler<GetPaymentSettingsQuery, List<PaymentSettingDto>>
{
    private readonly IPaymentSettingRepository _paymentSettingRepository;

    public GetPaymentSettingsQueryHandler(IPaymentSettingRepository paymentSettingRepository)
    {
        _paymentSettingRepository = paymentSettingRepository;
    }

    public async Task<List<PaymentSettingDto>> Handle(GetPaymentSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _paymentSettingRepository.GetAllAsync(cancellationToken);

        return settings.Select(s => new PaymentSettingDto
        {
            Id = s.Id,
            Provider = s.Provider,
            PublicKey = s.PublicKey,
            IsLive = s.IsLive,
            IsActive = s.IsActive,
            Settings = s.Settings
        }).ToList();
    }
}

using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Commands;

public record UpdatePaymentSettingCommand : IRequest<PaymentSettingDto>
{
    public string Provider { get; init; } = string.Empty;
    public string? TestModePublicKey { get; init; }
    public string? TestModeSecretKey { get; init; }
    public string? LiveModePublicKey { get; init; }
    public string? LiveModeSecretKey { get; init; }
    public string? WebhookSecret { get; init; }
    public bool IsLive { get; init; }
    public bool IsActive { get; init; }
}

public class UpdatePaymentSettingCommandHandler : IRequestHandler<UpdatePaymentSettingCommand, PaymentSettingDto>
{
    private readonly IPaymentSettingRepository _paymentSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentSettingCommandHandler(IPaymentSettingRepository paymentSettingRepository, IUnitOfWork unitOfWork)
    {
        _paymentSettingRepository = paymentSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentSettingDto> Handle(UpdatePaymentSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _paymentSettingRepository.GetByProviderAsync(request.Provider, cancellationToken);

        if (setting == null)
        {
            setting = new PaymentSetting
            {
                Id = Guid.NewGuid(),
                Provider = request.Provider,
                TestModePublicKey = request.TestModePublicKey,
                TestModeSecretKey = request.TestModeSecretKey,
                LiveModePublicKey = request.LiveModePublicKey,
                LiveModeSecretKey = request.LiveModeSecretKey,
                WebhookSecret = request.WebhookSecret,
                IsLive = request.IsLive,
                IsActive = request.IsActive,
                Settings = "{}"
            };
            await _paymentSettingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            if (!string.IsNullOrEmpty(request.TestModePublicKey))
            {
                setting.TestModePublicKey = request.TestModePublicKey;
            }
            if (!string.IsNullOrEmpty(request.TestModeSecretKey))
            {
                setting.TestModeSecretKey = request.TestModeSecretKey;
            }
            if (!string.IsNullOrEmpty(request.LiveModePublicKey))
            {
                setting.LiveModePublicKey = request.LiveModePublicKey;
            }
            if (!string.IsNullOrEmpty(request.LiveModeSecretKey))
            {
                setting.LiveModeSecretKey = request.LiveModeSecretKey;
            }
            if (!string.IsNullOrEmpty(request.WebhookSecret))
            {
                setting.WebhookSecret = request.WebhookSecret;
            }
            setting.IsLive = request.IsLive;
            setting.IsActive = request.IsActive;
            setting.UpdatedAt = DateTime.UtcNow;
            await _paymentSettingRepository.UpdateAsync(setting, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PaymentSettingDto
        {
            Id = setting.Id,
            Provider = setting.Provider,
            TestModePublicKey = setting.TestModePublicKey,
            LiveModePublicKey = setting.LiveModePublicKey,
            WebhookSecret = setting.WebhookSecret,
            IsLive = setting.IsLive,
            IsActive = setting.IsActive
        };
    }
}

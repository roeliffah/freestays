using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Commands;

public record UpdatePaymentSettingCommand : IRequest<PaymentSettingDto>
{
    public string Provider { get; init; } = string.Empty;
    public string? PublicKey { get; init; }
    public string? SecretKey { get; init; }
    public string? WebhookSecret { get; init; }
    public bool IsLive { get; init; }
    public bool IsActive { get; init; }
    public string? Settings { get; init; }
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
                PublicKey = request.PublicKey,
                SecretKey = request.SecretKey,
                WebhookSecret = request.WebhookSecret,
                IsLive = request.IsLive,
                IsActive = request.IsActive,
                Settings = request.Settings
            };
            await _paymentSettingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.PublicKey = request.PublicKey;
            if (!string.IsNullOrEmpty(request.SecretKey))
            {
                setting.SecretKey = request.SecretKey;
            }
            if (!string.IsNullOrEmpty(request.WebhookSecret))
            {
                setting.WebhookSecret = request.WebhookSecret;
            }
            setting.IsLive = request.IsLive;
            setting.IsActive = request.IsActive;
            setting.Settings = request.Settings;
            setting.UpdatedAt = DateTime.UtcNow;
            await _paymentSettingRepository.UpdateAsync(setting, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PaymentSettingDto
        {
            Id = setting.Id,
            Provider = setting.Provider,
            PublicKey = setting.PublicKey,
            IsLive = setting.IsLive,
            IsActive = setting.IsActive,
            Settings = setting.Settings
        };
    }
}

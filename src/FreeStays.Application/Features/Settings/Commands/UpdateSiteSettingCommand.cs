using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Commands;

public record UpdateSiteSettingCommand : IRequest<SiteSettingDto>
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Group { get; init; } = "general";
}

public class UpdateSiteSettingCommandHandler : IRequestHandler<UpdateSiteSettingCommand, SiteSettingDto>
{
    private readonly ISiteSettingRepository _siteSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSiteSettingCommandHandler(ISiteSettingRepository siteSettingRepository, IUnitOfWork unitOfWork)
    {
        _siteSettingRepository = siteSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SiteSettingDto> Handle(UpdateSiteSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _siteSettingRepository.GetByKeyAsync(request.Key, cancellationToken);

        if (setting == null)
        {
            setting = new SiteSetting
            {
                Id = Guid.NewGuid(),
                Key = request.Key,
                Value = request.Value,
                Group = request.Group
            };
            await _siteSettingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = request.Value;
            setting.UpdatedAt = DateTime.UtcNow;
            await _siteSettingRepository.UpdateAsync(setting, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SiteSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            Group = setting.Group,
            UpdatedAt = setting.UpdatedAt
        };
    }
}

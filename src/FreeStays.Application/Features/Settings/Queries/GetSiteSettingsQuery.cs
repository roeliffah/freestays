using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Queries;

public record GetSiteSettingsQuery(string? Group = null) : IRequest<List<SiteSettingDto>>;

public class GetSiteSettingsQueryHandler : IRequestHandler<GetSiteSettingsQuery, List<SiteSettingDto>>
{
    private readonly ISiteSettingRepository _siteSettingRepository;

    public GetSiteSettingsQueryHandler(ISiteSettingRepository siteSettingRepository)
    {
        _siteSettingRepository = siteSettingRepository;
    }

    public async Task<List<SiteSettingDto>> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = string.IsNullOrEmpty(request.Group)
            ? await _siteSettingRepository.GetAllAsync(cancellationToken)
            : await _siteSettingRepository.GetByGroupAsync(request.Group, cancellationToken);

        return settings.Select(s => new SiteSettingDto
        {
            Id = s.Id,
            Key = s.Key,
            Value = s.Value,
            Group = s.Group,
            UpdatedAt = s.UpdatedAt
        }).ToList();
    }
}

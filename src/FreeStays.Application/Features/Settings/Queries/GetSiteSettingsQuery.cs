using System.Text.Json;
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
            Value = ExtractJsonValue(s.Value),
            Group = s.Group,
            UpdatedAt = s.UpdatedAt
        }).ToList();
    }
    
    private static string ExtractJsonValue(string jsonValue)
    {
        try
        {
            // Try to deserialize as a JSON string
            var deserialized = JsonSerializer.Deserialize<string>(jsonValue);
            return deserialized ?? jsonValue;
        }
        catch (JsonException)
        {
            // If it's not a JSON string, return as is
            return jsonValue;
        }
    }
}

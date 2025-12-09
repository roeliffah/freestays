using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Queries;

public record GetSeoSettingsQuery(string Locale) : IRequest<List<SeoSettingDto>>;

public class GetSeoSettingsQueryHandler : IRequestHandler<GetSeoSettingsQuery, List<SeoSettingDto>>
{
    private readonly ISeoSettingRepository _seoSettingRepository;

    public GetSeoSettingsQueryHandler(ISeoSettingRepository seoSettingRepository)
    {
        _seoSettingRepository = seoSettingRepository;
    }

    public async Task<List<SeoSettingDto>> Handle(GetSeoSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _seoSettingRepository.GetByLocaleAsync(request.Locale, cancellationToken);

        return settings.Select(s => new SeoSettingDto
        {
            Id = s.Id,
            Locale = s.Locale,
            PageType = s.PageType,
            MetaTitle = s.MetaTitle,
            MetaDescription = s.MetaDescription,
            MetaKeywords = s.MetaKeywords,
            OgImage = s.OgImage
        }).ToList();
    }
}

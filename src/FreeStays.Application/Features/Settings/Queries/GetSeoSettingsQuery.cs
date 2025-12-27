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
            OgImage = s.OgImage,
            OgType = s.OgType,
            OgUrl = s.OgUrl,
            OgSiteName = s.OgSiteName,
            OgLocale = s.OgLocale,
            TwitterCard = s.TwitterCard,
            TwitterImage = s.TwitterImage,
            TwitterSite = s.TwitterSite,
            TwitterCreator = s.TwitterCreator,
            OrganizationName = s.OrganizationName,
            OrganizationUrl = s.OrganizationUrl,
            OrganizationLogo = s.OrganizationLogo,
            OrganizationDescription = s.OrganizationDescription,
            OrganizationSocialProfiles = s.OrganizationSocialProfiles,
            WebsiteName = s.WebsiteName,
            WebsiteUrl = s.WebsiteUrl,
            WebsiteSearchActionTarget = s.WebsiteSearchActionTarget,
            ContactPhone = s.ContactPhone,
            ContactEmail = s.ContactEmail,
            BusinessAddress = s.BusinessAddress,
            HotelSchemaType = s.HotelSchemaType,
            HotelName = s.HotelName,
            HotelImage = s.HotelImage,
            HotelAddress = s.HotelAddress,
            HotelTelephone = s.HotelTelephone,
            HotelPriceRange = s.HotelPriceRange,
            HotelStarRating = s.HotelStarRating,
            HotelAggregateRating = s.HotelAggregateRating,
            EnableSearchActionSchema = s.EnableSearchActionSchema,
            SearchActionTarget = s.SearchActionTarget,
            EnableFaqSchema = s.EnableFaqSchema,
            StructuredDataJson = s.StructuredDataJson
        }).ToList();
    }
}

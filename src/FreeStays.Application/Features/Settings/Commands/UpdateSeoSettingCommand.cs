using System.Text.Json;
using FreeStays.Application.DTOs.Settings;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Settings.Commands;

public record UpdateSeoSettingCommand : IRequest<SeoSettingDto>
{
    public string Locale { get; init; } = string.Empty;
    public string PageType { get; init; } = string.Empty;

    // Basic Meta Tags
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }

    // Open Graph
    public string? OgImage { get; init; }
    public string? OgType { get; init; }
    public string? OgUrl { get; init; }
    public string? OgSiteName { get; init; }
    public string? OgLocale { get; init; }

    // Twitter Card
    public string? TwitterCard { get; init; }
    public string? TwitterImage { get; init; }
    public string? TwitterSite { get; init; }
    public string? TwitterCreator { get; init; }

    // Organization Schema
    public string? OrganizationName { get; init; }
    public string? OrganizationUrl { get; init; }
    public string? OrganizationLogo { get; init; }
    public string? OrganizationDescription { get; init; }
    public string? OrganizationSocialProfiles { get; init; }

    // Website Schema
    public string? WebsiteName { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? WebsiteSearchActionTarget { get; init; }

    // Contact Info
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public string? BusinessAddress { get; init; }

    // Hotel Schema
    public string? HotelSchemaType { get; init; }
    public string? HotelName { get; init; }
    public string? HotelImage { get; init; }
    public string? HotelAddress { get; init; }
    public string? HotelTelephone { get; init; }
    public string? HotelPriceRange { get; init; }
    public int? HotelStarRating { get; init; }
    public string? HotelAggregateRating { get; init; }

    // Search Page Schema
    public bool? EnableSearchActionSchema { get; init; }
    public string? SearchActionTarget { get; init; }

    // FAQ Page Schema
    public bool? EnableFaqSchema { get; init; }

    // Custom Structured Data
    public string? StructuredDataJson { get; init; }
}

public class UpdateSeoSettingCommandHandler : IRequestHandler<UpdateSeoSettingCommand, SeoSettingDto>
{
    private readonly ISeoSettingRepository _seoSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSeoSettingCommandHandler(ISeoSettingRepository seoSettingRepository, IUnitOfWork unitOfWork)
    {
        _seoSettingRepository = seoSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SeoSettingDto> Handle(UpdateSeoSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _seoSettingRepository.GetByLocaleAndPageTypeAsync(request.Locale, request.PageType, cancellationToken);

        if (setting == null)
        {
            setting = new SeoSetting
            {
                Id = Guid.NewGuid(),
                Locale = request.Locale,
                PageType = request.PageType,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                MetaKeywords = request.MetaKeywords,
                OgImage = request.OgImage,
                OgType = request.OgType,
                OgUrl = request.OgUrl,
                OgSiteName = request.OgSiteName,
                OgLocale = request.OgLocale,
                TwitterCard = request.TwitterCard,
                TwitterImage = request.TwitterImage,
                TwitterSite = request.TwitterSite,
                TwitterCreator = request.TwitterCreator,
                OrganizationName = request.OrganizationName,
                OrganizationUrl = request.OrganizationUrl,
                OrganizationLogo = request.OrganizationLogo,
                OrganizationDescription = request.OrganizationDescription,
                OrganizationSocialProfiles = request.OrganizationSocialProfiles,
                WebsiteName = request.WebsiteName,
                WebsiteUrl = request.WebsiteUrl,
                WebsiteSearchActionTarget = request.WebsiteSearchActionTarget,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                BusinessAddress = request.BusinessAddress,
                HotelSchemaType = request.HotelSchemaType,
                HotelName = request.HotelName,
                HotelImage = request.HotelImage,
                HotelAddress = request.HotelAddress,
                HotelTelephone = request.HotelTelephone,
                HotelPriceRange = request.HotelPriceRange,
                HotelStarRating = request.HotelStarRating,
                HotelAggregateRating = request.HotelAggregateRating,
                EnableSearchActionSchema = request.EnableSearchActionSchema ?? false,
                SearchActionTarget = request.SearchActionTarget,
                EnableFaqSchema = request.EnableFaqSchema ?? false,
                StructuredDataJson = request.StructuredDataJson
            };
            await _seoSettingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.MetaTitle = request.MetaTitle;
            setting.MetaDescription = request.MetaDescription;
            setting.MetaKeywords = request.MetaKeywords;
            setting.OgImage = request.OgImage;
            setting.OgType = request.OgType;
            setting.OgUrl = request.OgUrl;
            setting.OgSiteName = request.OgSiteName;
            setting.OgLocale = request.OgLocale;
            setting.TwitterCard = request.TwitterCard;
            setting.TwitterImage = request.TwitterImage;
            setting.TwitterSite = request.TwitterSite;
            setting.TwitterCreator = request.TwitterCreator;
            setting.OrganizationName = request.OrganizationName;
            setting.OrganizationUrl = request.OrganizationUrl;
            setting.OrganizationLogo = request.OrganizationLogo;
            setting.OrganizationDescription = request.OrganizationDescription;
            setting.OrganizationSocialProfiles = request.OrganizationSocialProfiles;
            setting.WebsiteName = request.WebsiteName;
            setting.WebsiteUrl = request.WebsiteUrl;
            setting.WebsiteSearchActionTarget = request.WebsiteSearchActionTarget;
            setting.ContactPhone = request.ContactPhone;
            setting.ContactEmail = request.ContactEmail;
            setting.BusinessAddress = request.BusinessAddress;
            setting.HotelSchemaType = request.HotelSchemaType;
            setting.HotelName = request.HotelName;
            setting.HotelImage = request.HotelImage;
            setting.HotelAddress = request.HotelAddress;
            setting.HotelTelephone = request.HotelTelephone;
            setting.HotelPriceRange = request.HotelPriceRange;
            setting.HotelStarRating = request.HotelStarRating;
            setting.HotelAggregateRating = request.HotelAggregateRating;
            if (request.EnableSearchActionSchema.HasValue)
                setting.EnableSearchActionSchema = request.EnableSearchActionSchema.Value;
            setting.SearchActionTarget = request.SearchActionTarget;
            if (request.EnableFaqSchema.HasValue)
                setting.EnableFaqSchema = request.EnableFaqSchema.Value;
            setting.StructuredDataJson = request.StructuredDataJson;
            setting.UpdatedAt = DateTime.UtcNow;
            await _seoSettingRepository.UpdateAsync(setting, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SeoSettingDto
        {
            Id = setting.Id,
            Locale = setting.Locale,
            PageType = setting.PageType,
            MetaTitle = setting.MetaTitle,
            MetaDescription = setting.MetaDescription,
            MetaKeywords = setting.MetaKeywords,
            OgImage = setting.OgImage,
            OgType = setting.OgType,
            OgUrl = setting.OgUrl,
            OgSiteName = setting.OgSiteName,
            OgLocale = setting.OgLocale,
            TwitterCard = setting.TwitterCard,
            TwitterImage = setting.TwitterImage,
            TwitterSite = setting.TwitterSite,
            TwitterCreator = setting.TwitterCreator,
            OrganizationName = setting.OrganizationName,
            OrganizationUrl = setting.OrganizationUrl,
            OrganizationLogo = setting.OrganizationLogo,
            OrganizationDescription = setting.OrganizationDescription,
            OrganizationSocialProfiles = setting.OrganizationSocialProfiles,
            WebsiteName = setting.WebsiteName,
            WebsiteUrl = setting.WebsiteUrl,
            WebsiteSearchActionTarget = setting.WebsiteSearchActionTarget,
            ContactPhone = setting.ContactPhone,
            ContactEmail = setting.ContactEmail,
            BusinessAddress = setting.BusinessAddress,
            HotelSchemaType = setting.HotelSchemaType,
            HotelName = setting.HotelName,
            HotelImage = setting.HotelImage,
            HotelAddress = setting.HotelAddress,
            HotelTelephone = setting.HotelTelephone,
            HotelPriceRange = setting.HotelPriceRange,
            HotelStarRating = setting.HotelStarRating,
            HotelAggregateRating = setting.HotelAggregateRating,
            EnableSearchActionSchema = setting.EnableSearchActionSchema,
            SearchActionTarget = setting.SearchActionTarget,
            EnableFaqSchema = setting.EnableFaqSchema,
            StructuredDataJson = setting.StructuredDataJson
        };
    }
}

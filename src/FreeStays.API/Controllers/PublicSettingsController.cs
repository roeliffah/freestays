using FreeStays.Application.Features.Settings.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

[Route("api/v1/public/settings")]
public class PublicSettingsController : BaseApiController
{
    /// <summary>
    /// Frontend için genel site ayarlarını getir (anonim erişim)
    /// </summary>
    [HttpGet("site")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicSiteSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery());
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            siteName = settingsDict.GetValueOrDefault("siteName", "FreeStays"),
            tagline = settingsDict.GetValueOrDefault("tagline", "Your Dream Stay, Our Mission"),
            supportEmail = settingsDict.GetValueOrDefault("supportEmail", "support@freestays.com"),
            supportPhone = settingsDict.GetValueOrDefault("supportPhone", "+90 555 123 4567"),
            defaultLocale = settingsDict.GetValueOrDefault("defaultLocale", "tr"),
            availableLocales = new[] { "tr", "en", "de", "fr" },
            defaultCurrency = settingsDict.GetValueOrDefault("defaultCurrency", "TRY"),
            availableCurrencies = new[] { "TRY", "USD", "EUR", "GBP" },
            logoUrl = settingsDict.GetValueOrDefault("logoUrl", "/images/logo.png"),
            faviconUrl = settingsDict.GetValueOrDefault("faviconUrl", "/images/favicon.ico")
        });
    }

    /// <summary>
    /// Frontend için affiliate programs ayarlarını getir (anonim erişim)
    /// Widget kodları ve affiliate linkleri için kullanılır
    /// </summary>
    [HttpGet("affiliate-programs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAffiliatePrograms()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery());
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            excursions = new
            {
                active = settingsDict.GetValueOrDefault("excursionsActive", "false") == "true",
                affiliateCode = settingsDict.GetValueOrDefault("excursionsAffiliateCode", null),
                widgetCode = settingsDict.GetValueOrDefault("excursionsWidgetCode", null)
            },
            carRental = new
            {
                active = settingsDict.GetValueOrDefault("carRentalActive", "false") == "true",
                affiliateCode = settingsDict.GetValueOrDefault("carRentalAffiliateCode", null),
                widgetCode = settingsDict.GetValueOrDefault("carRentalWidgetCode", null)
            },
            flightBooking = new
            {
                active = settingsDict.GetValueOrDefault("flightBookingActive", "false") == "true",
                affiliateCode = settingsDict.GetValueOrDefault("flightBookingAffiliateCode", null),
                widgetCode = settingsDict.GetValueOrDefault("flightBookingWidgetCode", null)
            }
        });
    }

    /// <summary>
    /// Frontend için sosyal medya linklerini getir (anonim erişim)
    /// </summary>
    [HttpGet("social")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSocialLinks()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("social"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            facebook = settingsDict.GetValueOrDefault("facebook", null),
            twitter = settingsDict.GetValueOrDefault("twitter", null),
            instagram = settingsDict.GetValueOrDefault("instagram", null),
            linkedin = settingsDict.GetValueOrDefault("linkedin", null),
            youtube = settingsDict.GetValueOrDefault("youtube", null)
        });
    }
}

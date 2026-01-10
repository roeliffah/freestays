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
            // Site Temel Bilgileri
            siteName = settingsDict.GetValueOrDefault("siteName", "FreeStays"),
            tagline = settingsDict.GetValueOrDefault("tagline", "Your Dream Stay, Our Mission"),
            siteUrl = settingsDict.GetValueOrDefault("siteUrl", "https://freestays.com"),
            logoUrl = settingsDict.GetValueOrDefault("logoUrl", "/images/logo.png"),
            faviconUrl = settingsDict.GetValueOrDefault("faviconUrl", "/images/favicon.ico"),
            maintenanceMode = settingsDict.GetValueOrDefault("maintenanceMode", "false") == "true",
            maintenanceMessage = settingsDict.GetValueOrDefault("maintenanceMessage", "Site is under maintenance. Please check back later."),

            // İletişim Bilgileri
            supportEmail = settingsDict.GetValueOrDefault("supportEmail", "support@freestays.com"),
            supportPhone = settingsDict.GetValueOrDefault("supportPhone", "+90 555 123 4567"),
            contactEmail = settingsDict.GetValueOrDefault("contactEmail", "contact@freestays.com"),

            // Firma/İşletme Bilgileri
            companyName = settingsDict.GetValueOrDefault("companyName", "FreeStays Ltd."),
            businessAddress = settingsDict.GetValueOrDefault("businessAddress", ""),
            businessPhone = settingsDict.GetValueOrDefault("businessPhone", ""),
            taxId = settingsDict.GetValueOrDefault("taxId", ""),
            registrationNumber = settingsDict.GetValueOrDefault("registrationNumber", ""),

            // Dil ve Para Birimi
            defaultLocale = settingsDict.GetValueOrDefault("defaultLocale", "tr"),
            availableLocales = new[] { "tr", "en", "de", "fr" },
            defaultCurrency = settingsDict.GetValueOrDefault("defaultCurrency", "TRY"),
            availableCurrencies = new[] { "TRY", "USD", "EUR", "GBP" },
            timezone = settingsDict.GetValueOrDefault("timezone", "Europe/Amsterdam"),

            // Coupon Prices
            oneTimeCouponPrice = decimal.TryParse(settingsDict.GetValueOrDefault("oneTimePriceEUR", "19.99"), out var oneTimePrice) ? oneTimePrice : 19.99m,
            annualCouponPrice = decimal.TryParse(settingsDict.GetValueOrDefault("annualPriceEUR", "99.99"), out var annualPrice) ? annualPrice : 99.99m,

            // Stripe Payment (Public Key Only)
            stripePublicKey = settingsDict.GetValueOrDefault("stripePublicKey", ""),

            // Pricing Information
            profitMargin = decimal.TryParse(settingsDict.GetValueOrDefault("profitMargin", "10"), out var margin) ? margin : 10m,
            defaultVatRate = decimal.TryParse(settingsDict.GetValueOrDefault("defaultVatRate", "20"), out var vat) ? vat : 20m,
            extraFee = decimal.TryParse(settingsDict.GetValueOrDefault("extraFee", "0"), out var extra) ? extra : 0m,
            discountRate = decimal.TryParse(settingsDict.GetValueOrDefault("discountRate", "0"), out var discount) ? discount : 0m,

            // İletişim Detayları
            contact = new
            {
                email = settingsDict.GetValueOrDefault("email", null),
                phone = settingsDict.GetValueOrDefault("phone", null),
                whatsapp = settingsDict.GetValueOrDefault("whatsapp", null),
                address = settingsDict.GetValueOrDefault("address", null),
                city = settingsDict.GetValueOrDefault("city", null),
                country = settingsDict.GetValueOrDefault("country", null),
                postalCode = settingsDict.GetValueOrDefault("postalCode", null),
                workingHours = settingsDict.GetValueOrDefault("workingHours", null),
                mapLatitude = decimal.TryParse(settingsDict.GetValueOrDefault("mapLatitude", null)?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) ? lat : (decimal?)null,
                mapLongitude = decimal.TryParse(settingsDict.GetValueOrDefault("mapLongitude", null)?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng) ? lng : (decimal?)null,
                googleMapsIframe = settingsDict.GetValueOrDefault("googleMapsIframe", null)
            },

            // Statik Sayfalar
            privacyPolicy = settingsDict.GetValueOrDefault("privacyPolicy", ""),
            termsOfService = settingsDict.GetValueOrDefault("termsOfService", ""),
            cancellationPolicy = settingsDict.GetValueOrDefault("cancellationPolicy", ""),

            // Sosyal Medya Linkleri
            social = new
            {
                facebook = settingsDict.GetValueOrDefault("facebook", null),
                twitter = settingsDict.GetValueOrDefault("twitter", null),
                instagram = settingsDict.GetValueOrDefault("instagram", null),
                linkedin = settingsDict.GetValueOrDefault("linkedin", null),
                youtube = settingsDict.GetValueOrDefault("youtube", null)
            }
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
    /// Frontend için iletişim ve yasal bilgileri getir (anonim erişim)
    /// </summary>
    [HttpGet("contact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContactSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("contact"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            email = settingsDict.GetValueOrDefault("email", null),
            phone = settingsDict.GetValueOrDefault("phone", null),
            whatsapp = settingsDict.GetValueOrDefault("whatsapp", null),
            address = settingsDict.GetValueOrDefault("address", null),
            city = settingsDict.GetValueOrDefault("city", null),
            country = settingsDict.GetValueOrDefault("country", null),
            postalCode = settingsDict.GetValueOrDefault("postalCode", null),
            workingHours = settingsDict.GetValueOrDefault("workingHours", null),
            mapLatitude = decimal.TryParse(settingsDict.GetValueOrDefault("mapLatitude", null)?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) ? lat : (decimal?)null,
            mapLongitude = decimal.TryParse(settingsDict.GetValueOrDefault("mapLongitude", null)?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng) ? lng : (decimal?)null,
            googleMapsIframe = settingsDict.GetValueOrDefault("googleMapsIframe", null),
            privacyPolicy = settingsDict.GetValueOrDefault("privacyPolicy", null),
            termsOfService = settingsDict.GetValueOrDefault("termsOfService", null),
            cancellationPolicy = settingsDict.GetValueOrDefault("cancellationPolicy", null)
        });
    }
}

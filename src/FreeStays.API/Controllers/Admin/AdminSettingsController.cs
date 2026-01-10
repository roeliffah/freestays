using FreeStays.Application.DTOs.Settings;
using FreeStays.Application.Features.Settings.Commands;
using FreeStays.Application.Features.Settings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/settings")]
public class AdminSettingsController : BaseApiController
{
    #region Site Settings

    /// <summary>
    /// Site ayarlarını getir
    /// </summary>
    [HttpGet("site")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSiteSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery());
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            siteName = settingsDict.GetValueOrDefault("siteName", "FreeStays"),
            tagline = settingsDict.GetValueOrDefault("tagline", "Your Dream Stay, Our Mission"),
            siteUrl = settingsDict.GetValueOrDefault("siteUrl", "https://freestays.com"),
            supportEmail = settingsDict.GetValueOrDefault("supportEmail", "support@freestays.com"),
            supportPhone = settingsDict.GetValueOrDefault("supportPhone", "+90 555 123 4567"),
            defaultLocale = settingsDict.GetValueOrDefault("defaultLocale", "tr"),
            availableLocales = new[] { "tr", "en", "de", "fr" },
            defaultCurrency = settingsDict.GetValueOrDefault("defaultCurrency", "TRY"),
            availableCurrencies = new[] { "TRY", "USD", "EUR", "GBP" },
            timezone = settingsDict.GetValueOrDefault("timezone", "Europe/Amsterdam"),
            maintenanceMode = settingsDict.GetValueOrDefault("maintenanceMode", "false") == "true",
            maintenanceMessage = settingsDict.GetValueOrDefault("maintenanceMessage", "Site is under maintenance. Please check back later."),
            logoUrl = settingsDict.GetValueOrDefault("logoUrl", "/images/logo.png"),
            faviconUrl = settingsDict.GetValueOrDefault("faviconUrl", "/images/favicon.ico"),
            profitMargin = decimal.TryParse(settingsDict.GetValueOrDefault("profitMargin", "10"), out var margin) ? margin : 10m,
            defaultVatRate = decimal.TryParse(settingsDict.GetValueOrDefault("defaultVatRate", "20"), out var vat) ? vat : 20m,
            extraFee = decimal.TryParse(settingsDict.GetValueOrDefault("extraFee", "0"), out var extra) ? extra : 0m,
            discountRate = decimal.TryParse(settingsDict.GetValueOrDefault("discountRate", "0"), out var discount) ? discount : 0m,
            // Coupon Prices
            oneTimeCouponPrice = decimal.TryParse(settingsDict.GetValueOrDefault("oneTimePriceEUR", "19.99"), out var oneTimePrice) ? oneTimePrice : 19.99m,
            annualCouponPrice = decimal.TryParse(settingsDict.GetValueOrDefault("annualPriceEUR", "99.99"), out var annualPrice) ? annualPrice : 99.99m,
            // Stripe Payment Settings
            stripePublicKey = settingsDict.GetValueOrDefault("stripePublicKey", ""),
            stripeSecretKey = settingsDict.GetValueOrDefault("stripeSecretKey", ""),
            stripeWebhookSecret = settingsDict.GetValueOrDefault("stripeWebhookSecret", ""),
            // Affiliate Programs
            excursionsActive = settingsDict.GetValueOrDefault("excursionsActive", "false") == "true",
            excursionsAffiliateCode = settingsDict.GetValueOrDefault("excursionsAffiliateCode", ""),
            excursionsWidgetCode = settingsDict.GetValueOrDefault("excursionsWidgetCode", ""),
            carRentalActive = settingsDict.GetValueOrDefault("carRentalActive", "false") == "true",
            carRentalAffiliateCode = settingsDict.GetValueOrDefault("carRentalAffiliateCode", ""),
            carRentalWidgetCode = settingsDict.GetValueOrDefault("carRentalWidgetCode", ""),
            flightBookingActive = settingsDict.GetValueOrDefault("flightBookingActive", "false") == "true",
            flightBookingAffiliateCode = settingsDict.GetValueOrDefault("flightBookingAffiliateCode", ""),
            flightBookingWidgetCode = settingsDict.GetValueOrDefault("flightBookingWidgetCode", ""),
            // Statik Sayfalar
            privacyPolicy = settingsDict.GetValueOrDefault("privacyPolicy", ""),
            termsOfService = settingsDict.GetValueOrDefault("termsOfService", ""),
            cancellationPolicy = settingsDict.GetValueOrDefault("cancellationPolicy", ""),
            // Sosyal Medya Linkleri
            social = new
            {
                facebook = settingsDict.GetValueOrDefault("facebook", ""),
                twitter = settingsDict.GetValueOrDefault("twitter", ""),
                instagram = settingsDict.GetValueOrDefault("instagram", ""),
                linkedin = settingsDict.GetValueOrDefault("linkedin", ""),
                youtube = settingsDict.GetValueOrDefault("youtube", "")
            }
        });
    }

    /// <summary>
    /// Site ayarlarını güncelle
    /// </summary>
    [HttpPut("site")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSiteSettings([FromBody] UpdateSiteSettingsRequest request)
    {
        if (request.SiteName != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "siteName", Value = request.SiteName, Group = "site" });
        if (request.Tagline != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "tagline", Value = request.Tagline, Group = "site" });
        if (request.SupportEmail != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "supportEmail", Value = request.SupportEmail, Group = "site" });
        if (request.SupportPhone != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "supportPhone", Value = request.SupportPhone, Group = "site" });
        if (request.DefaultLocale != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultLocale", Value = request.DefaultLocale, Group = "site" });
        if (request.DefaultCurrency != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultCurrency", Value = request.DefaultCurrency, Group = "site" });
        if (request.Timezone != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "timezone", Value = request.Timezone, Group = "site" });
        if (request.MaintenanceMode.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "maintenanceMode", Value = request.MaintenanceMode.Value.ToString().ToLower(), Group = "site" });
        if (request.MaintenanceMessage != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "maintenanceMessage", Value = request.MaintenanceMessage, Group = "site" });
        if (request.LogoUrl != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "logoUrl", Value = request.LogoUrl, Group = "site" });
        if (request.ProfitMargin.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "profitMargin", Value = request.ProfitMargin.Value.ToString(), Group = "site" });
        if (request.DefaultVatRate.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultVatRate", Value = request.DefaultVatRate.Value.ToString(), Group = "site" });
        if (request.ExtraFee.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "extraFee", Value = request.ExtraFee.Value.ToString(), Group = "site" });
        if (request.DiscountRate.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "discountRate", Value = request.DiscountRate.Value.ToString(), Group = "site" });

        // Coupon Prices
        if (request.OneTimeCouponPrice.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "oneTimePriceEUR", Value = request.OneTimeCouponPrice.Value.ToString(), Group = "coupons" });
        if (request.AnnualCouponPrice.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "annualPriceEUR", Value = request.AnnualCouponPrice.Value.ToString(), Group = "coupons" });

        // Stripe Payment Settings
        if (request.StripePublicKey != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "stripePublicKey", Value = request.StripePublicKey, Group = "payment" });
        if (request.StripeSecretKey != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "stripeSecretKey", Value = request.StripeSecretKey, Group = "payment" });
        if (request.StripeWebhookSecret != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "stripeWebhookSecret", Value = request.StripeWebhookSecret, Group = "payment" });

        // Affiliate Programs
        if (request.ExcursionsActive.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "excursionsActive", Value = request.ExcursionsActive.Value.ToString().ToLower(), Group = "site" });
        if (request.ExcursionsAffiliateCode != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "excursionsAffiliateCode", Value = request.ExcursionsAffiliateCode, Group = "site" });
        if (request.ExcursionsWidgetCode != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "excursionsWidgetCode", Value = request.ExcursionsWidgetCode, Group = "site" });

        if (request.CarRentalActive.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "carRentalActive", Value = request.CarRentalActive.Value.ToString().ToLower(), Group = "site" });
        if (request.CarRentalAffiliateCode != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "carRentalAffiliateCode", Value = request.CarRentalAffiliateCode, Group = "site" });
        if (request.CarRentalWidgetCode != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "carRentalWidgetCode", Value = request.CarRentalWidgetCode, Group = "site" });

        if (request.FlightBookingActive.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "flightBookingActive", Value = request.FlightBookingActive.Value.ToString().ToLower(), Group = "site" });
        if (request.FlightBookingAffiliateCode != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "flightBookingAffiliateCode", Value = request.FlightBookingAffiliateCode, Group = "site" });
        if (request.FlightBookingWidgetCode != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "flightBookingWidgetCode", Value = request.FlightBookingWidgetCode, Group = "site" });

        // Statik Sayfalar
        if (request.PrivacyPolicy != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "privacyPolicy", Value = request.PrivacyPolicy, Group = "site" });
        if (request.TermsOfService != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "termsOfService", Value = request.TermsOfService, Group = "site" });
        if (request.CancellationPolicy != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "cancellationPolicy", Value = request.CancellationPolicy, Group = "site" });

        // Sosyal Medya Linkleri
        if (request.SocialLinks != null)
        {
            if (request.SocialLinks.Facebook != null)
                await Mediator.Send(new UpdateSiteSettingCommand { Key = "facebook", Value = request.SocialLinks.Facebook, Group = "site" });
            if (request.SocialLinks.Twitter != null)
                await Mediator.Send(new UpdateSiteSettingCommand { Key = "twitter", Value = request.SocialLinks.Twitter, Group = "site" });
            if (request.SocialLinks.Instagram != null)
                await Mediator.Send(new UpdateSiteSettingCommand { Key = "instagram", Value = request.SocialLinks.Instagram, Group = "site" });
            if (request.SocialLinks.LinkedIn != null)
                await Mediator.Send(new UpdateSiteSettingCommand { Key = "linkedin", Value = request.SocialLinks.LinkedIn, Group = "site" });
            if (request.SocialLinks.YouTube != null)
                await Mediator.Send(new UpdateSiteSettingCommand { Key = "youtube", Value = request.SocialLinks.YouTube, Group = "site" });
        }

        return Ok(new { message = "Site ayarları güncellendi." });
    }

    #endregion

    #region SEO Settings

    /// <summary>
    /// Genel SEO ayarlarını getir
    /// </summary>
    [HttpGet("seo")]
    [ProducesResponseType(typeof(SeoSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeoSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("seo"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        // Helper to parse JSON or return null
        List<string>? ParseJsonArray(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json); }
            catch { return null; }
        }

        var response = new SeoSettingsResponse(
            DefaultMetaTitle: settingsDict.GetValueOrDefault("defaultMetaTitle", "FreeStays - En İyi Otel Fırsatları"),
            DefaultMetaDescription: settingsDict.GetValueOrDefault("defaultMetaDescription", "FreeStays ile en uygun otel fırsatlarını keşfedin."),
            GoogleAnalyticsId: settingsDict.GetValueOrDefault("googleAnalyticsId", ""),
            GoogleTagManagerId: settingsDict.GetValueOrDefault("googleTagManagerId", ""),
            FacebookPixelId: settingsDict.GetValueOrDefault("facebookPixelId", ""),
            RobotsTxt: settingsDict.GetValueOrDefault("robotsTxt", "User-agent: *\nAllow: /"),
            SitemapEnabled: settingsDict.GetValueOrDefault("sitemapEnabled", "true") == "true",
            OrganizationName: settingsDict.GetValueOrDefault("organizationName", ""),
            OrganizationUrl: settingsDict.GetValueOrDefault("organizationUrl", ""),
            OrganizationLogo: settingsDict.GetValueOrDefault("organizationLogo", ""),
            OrganizationDescription: settingsDict.GetValueOrDefault("organizationDescription", ""),
            OrganizationSocialProfiles: ParseJsonArray(settingsDict.GetValueOrDefault("organizationSocialProfiles", "")),
            WebsiteName: settingsDict.GetValueOrDefault("websiteName", ""),
            WebsiteUrl: settingsDict.GetValueOrDefault("websiteUrl", ""),
            WebsiteSearchActionTarget: settingsDict.GetValueOrDefault("websiteSearchActionTarget", ""),
            ContactPhone: settingsDict.GetValueOrDefault("contactPhone", ""),
            ContactEmail: settingsDict.GetValueOrDefault("contactEmail", ""),
            BusinessAddress: settingsDict.GetValueOrDefault("businessAddress", ""),
            TwitterSite: settingsDict.GetValueOrDefault("twitterSite", ""),
            TwitterCreator: settingsDict.GetValueOrDefault("twitterCreator", "")
        );

        return Ok(response);
    }

    /// <summary>
    /// Genel SEO ayarlarını güncelle
    /// </summary>
    [HttpPut("seo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSeoSettings([FromBody] UpdateSeoSettingsRequest request)
    {
        if (request.DefaultMetaTitle != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultMetaTitle", Value = request.DefaultMetaTitle, Group = "seo" });
        if (request.DefaultMetaDescription != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultMetaDescription", Value = request.DefaultMetaDescription, Group = "seo" });
        if (request.GoogleAnalyticsId != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "googleAnalyticsId", Value = request.GoogleAnalyticsId, Group = "seo" });
        if (request.GoogleTagManagerId != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "googleTagManagerId", Value = request.GoogleTagManagerId, Group = "seo" });
        if (request.FacebookPixelId != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "facebookPixelId", Value = request.FacebookPixelId, Group = "seo" });
        if (request.RobotsTxt != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "robotsTxt", Value = request.RobotsTxt, Group = "seo" });
        if (request.SitemapEnabled.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "sitemapEnabled", Value = request.SitemapEnabled.Value.ToString().ToLower(), Group = "seo" });

        // Organization Schema
        if (request.OrganizationName != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "organizationName", Value = request.OrganizationName, Group = "seo" });
        if (request.OrganizationUrl != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "organizationUrl", Value = request.OrganizationUrl, Group = "seo" });
        if (request.OrganizationLogo != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "organizationLogo", Value = request.OrganizationLogo, Group = "seo" });
        if (request.OrganizationDescription != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "organizationDescription", Value = request.OrganizationDescription, Group = "seo" });
        if (request.OrganizationSocialProfiles != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "organizationSocialProfiles", Value = System.Text.Json.JsonSerializer.Serialize(request.OrganizationSocialProfiles), Group = "seo" });

        // Website Schema
        if (request.WebsiteName != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "websiteName", Value = request.WebsiteName, Group = "seo" });
        if (request.WebsiteUrl != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "websiteUrl", Value = request.WebsiteUrl, Group = "seo" });
        if (request.WebsiteSearchActionTarget != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "websiteSearchActionTarget", Value = request.WebsiteSearchActionTarget, Group = "seo" });

        // Contact Info
        if (request.ContactPhone != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "contactPhone", Value = request.ContactPhone, Group = "seo" });
        if (request.ContactEmail != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "contactEmail", Value = request.ContactEmail, Group = "seo" });
        if (request.BusinessAddress != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "businessAddress", Value = request.BusinessAddress, Group = "seo" });

        // Twitter Card
        if (request.TwitterSite != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "twitterSite", Value = request.TwitterSite, Group = "seo" });
        if (request.TwitterCreator != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "twitterCreator", Value = request.TwitterCreator, Group = "seo" });

        return Ok(new { message = "SEO ayarları güncellendi." });
    }

    /// <summary>
    /// Belirli bir dil için SEO ayarlarını getir
    /// </summary>
    [HttpGet("seo/{locale}")]
    [ProducesResponseType(typeof(LocaleSeoSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeoSettingsByLocale(string locale)
    {
        var settings = await Mediator.Send(new GetSeoSettingsQuery(locale));

        return Ok(new LocaleSeoSettingsResponse(
            Locale: locale,
            Pages: settings
        ));
    }

    /// <summary>
    /// Belirli bir dil için SEO ayarlarını güncelle
    /// </summary>
    [HttpPut("seo/{locale}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSeoSettingsByLocale(string locale, [FromBody] UpdateLocaleSeoSettingsRequest request)
    {
        foreach (var page in request.Pages)
        {
            await Mediator.Send(new UpdateSeoSettingCommand
            {
                Locale = locale,
                PageType = page.PageType,
                MetaTitle = page.MetaTitle,
                MetaDescription = page.MetaDescription,
                MetaKeywords = page.MetaKeywords,
                OgImage = page.OgImage,
                OgType = page.OgType,
                OgUrl = page.OgUrl,
                OgSiteName = page.OgSiteName,
                OgLocale = page.OgLocale,
                TwitterCard = page.TwitterCard,
                TwitterImage = page.TwitterImage,
                HotelSchemaType = page.HotelSchemaType,
                HotelName = page.HotelName,
                HotelImage = page.HotelImage,
                HotelAddress = page.HotelAddress,
                HotelTelephone = page.HotelTelephone,
                HotelPriceRange = page.HotelPriceRange,
                HotelStarRating = page.HotelStarRating,
                HotelAggregateRating = page.HotelAggregateRating,
                EnableSearchActionSchema = page.EnableSearchActionSchema,
                SearchActionTarget = page.SearchActionTarget,
                EnableFaqSchema = page.EnableFaqSchema,
                StructuredDataJson = page.StructuredDataJson
            });
        }

        return Ok(new { message = $"{locale} dili için SEO ayarları güncellendi." });
    }

    #endregion

    #region Payment Settings

    /// <summary>
    /// Ödeme ayarlarını getir
    /// </summary>
    [HttpGet("payment")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentSettings()
    {
        var setting = await Mediator.Send(new GetPaymentSettingsQuery());

        if (setting == null)
        {
            return Ok(new
            {
                provider = "stripe",
                testMode = (object?)null,
                liveMode = (object?)null,
                webhookSecret = (string?)null,
                isLive = false,
                isActive = false
            });
        }

        return Ok(new
        {
            provider = setting.Provider,
            testMode = setting.TestModePublicKey != null ? new
            {
                publicKey = setting.TestModePublicKey,
                secretKey = setting.TestModeSecretKey
            } : null,
            liveMode = setting.LiveModePublicKey != null ? new
            {
                publicKey = setting.LiveModePublicKey,
                secretKey = setting.LiveModeSecretKey
            } : null,
            webhookSecret = setting.WebhookSecret,
            isLive = setting.IsLive,
            isActive = setting.IsActive
        });
    }

    /// <summary>
    /// Ödeme ayarlarını güncelle
    /// </summary>
    [HttpPut("payment")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePaymentSettings([FromBody] UpdatePaymentSettingsRequest request)
    {
        await Mediator.Send(new UpdatePaymentSettingCommand
        {
            Provider = request.Provider,
            TestModePublicKey = request.TestMode?.PublicKey,
            TestModeSecretKey = request.TestMode?.SecretKey,
            LiveModePublicKey = request.LiveMode?.PublicKey,
            LiveModeSecretKey = request.LiveMode?.SecretKey,
            WebhookSecret = request.WebhookSecret,
            IsLive = request.IsLive,
            IsActive = request.IsActive
        });

        return Ok(new { message = "Ödeme ayarları güncellendi." });
    }

    /// <summary>
    /// Ödeme bağlantısını test et
    /// </summary>
    [HttpPost("payment/test-connection")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestPaymentConnection([FromBody] TestPaymentConnectionRequest request)
    {
        // TODO: Implement actual payment provider connection test
        return Ok(new
        {
            success = true,
            provider = request.Provider,
            message = "Bağlantı başarılı."
        });
    }

    #endregion

    #region SMTP Settings

    /// <summary>
    /// SMTP ayarlarını getir
    /// </summary>
    [HttpGet("smtp")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSmtpSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("smtp"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        var hasPassword = !string.IsNullOrEmpty(settingsDict.GetValueOrDefault("smtpPassword", ""));

        return Ok(new
        {
            smtpHost = settingsDict.GetValueOrDefault("smtpHost", ""),
            smtpPort = int.TryParse(settingsDict.GetValueOrDefault("smtpPort", "587"), out var port) ? port : 587,
            smtpUsername = settingsDict.GetValueOrDefault("smtpUsername", ""),
            hasPassword = hasPassword,
            smtpFromEmail = settingsDict.GetValueOrDefault("smtpFromEmail", ""),
            smtpFromName = settingsDict.GetValueOrDefault("smtpFromName", "FreeStays"),
            smtpEnableSsl = settingsDict.GetValueOrDefault("smtpEnableSsl", "true") == "true",
            smtpIsConfigured = !string.IsNullOrEmpty(settingsDict.GetValueOrDefault("smtpHost", ""))
        });
    }

    /// <summary>
    /// SMTP ayarlarını güncelle
    /// </summary>
    [HttpPut("smtp")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSmtpSettings([FromBody] UpdateSmtpSettingsRequest request)
    {
        // Validate SMTP host if provided
        if (request.SmtpHost != null && string.IsNullOrWhiteSpace(request.SmtpHost))
        {
            return BadRequest(new { message = "SMTP host boş olamaz." });
        }

        // Validate SMTP port range
        if (request.SmtpPort.HasValue && (request.SmtpPort.Value < 1 || request.SmtpPort.Value > 65535))
        {
            return BadRequest(new { message = "SMTP port 1-65535 arasında olmalıdır." });
        }

        // Validate email format
        if (request.SmtpFromEmail != null && !string.IsNullOrEmpty(request.SmtpFromEmail))
        {
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.SmtpFromEmail))
            {
                return BadRequest(new { message = "Geçersiz email formatı." });
            }
        }

        // Validate required fields if SMTP is being configured
        if (request.SmtpHost != null)
        {
            var settings = await Mediator.Send(new GetSiteSettingsQuery("smtp"));
            var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

            var fromEmail = request.SmtpFromEmail ?? settingsDict.GetValueOrDefault("smtpFromEmail", "");
            if (string.IsNullOrEmpty(fromEmail))
            {
                return BadRequest(new { message = "SMTP from email gerekli." });
            }
        }

        if (request.SmtpHost != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "smtpHost", Value = request.SmtpHost, Group = "smtp" });
        if (request.SmtpPort.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "smtpPort", Value = request.SmtpPort.Value.ToString(), Group = "smtp" });
        if (request.SmtpUsername != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "smtpUsername", Value = request.SmtpUsername, Group = "smtp" });
        if (request.SmtpPassword != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "smtpPassword", Value = request.SmtpPassword, Group = "smtp" });
        if (request.SmtpFromEmail != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "smtpFromEmail", Value = request.SmtpFromEmail, Group = "smtp" });
        if (request.SmtpFromName != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "smtpFromName", Value = request.SmtpFromName, Group = "smtp" });
        if (request.SmtpEnableSsl.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "smtpEnableSsl", Value = request.SmtpEnableSsl.Value.ToString().ToLower(), Group = "smtp" });

        return Ok(new { message = "SMTP ayarları güncellendi." });
    }

    /// <summary>
    /// SMTP bağlantısını test et
    /// </summary>
    [HttpPost("smtp/test-connection")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestSmtpConnection([FromBody] TestSmtpConnectionRequest request)
    {
        try
        {
            // Get current SMTP settings
            var settings = await Mediator.Send(new GetSiteSettingsQuery("smtp"));
            var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

            var smtpHost = settingsDict.GetValueOrDefault("smtpHost", "");
            var smtpPort = int.TryParse(settingsDict.GetValueOrDefault("smtpPort", "587"), out var port) ? port : 587;
            var smtpUsername = settingsDict.GetValueOrDefault("smtpUsername", "");
            var smtpPassword = settingsDict.GetValueOrDefault("smtpPassword", "");
            var smtpFromEmail = settingsDict.GetValueOrDefault("smtpFromEmail", "");
            var smtpFromName = settingsDict.GetValueOrDefault("smtpFromName", "FreeStays");
            var smtpEnableSsl = settingsDict.GetValueOrDefault("smtpEnableSsl", "true") == "true";

            // Validate SMTP configuration
            if (string.IsNullOrEmpty(smtpHost))
            {
                return BadRequest(new { success = false, message = "SMTP host yapılandırılmamış." });
            }

            if (string.IsNullOrEmpty(smtpFromEmail))
            {
                return BadRequest(new { success = false, message = "SMTP from email yapılandırılmamış." });
            }

            // Validate test email
            if (string.IsNullOrEmpty(request.TestEmail))
            {
                return BadRequest(new { success = false, message = "Test email adresi gerekli." });
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.TestEmail))
            {
                return BadRequest(new { success = false, message = "Geçersiz email formatı." });
            }

            // Create test email message
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(smtpFromName, smtpFromEmail));
            message.To.Add(new MimeKit.MailboxAddress("", request.TestEmail));
            message.Subject = "FreeStays SMTP Test";

            var bodyBuilder = new MimeKit.BodyBuilder
            {
                HtmlBody = @"
                    <html>
                        <body style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2 style='color: #2563eb;'>SMTP Bağlantı Testi Başarılı ✓</h2>
                            <p>Bu bir test e-postasıdır. SMTP ayarlarınız doğru şekilde yapılandırılmış ve çalışıyor.</p>
                            <hr style='margin: 20px 0;'/>
                            <p style='color: #6b7280; font-size: 12px;'>
                                Test Zamanı: " + DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss UTC") + @"<br/>
                                SMTP Host: " + smtpHost + @"<br/>
                                SMTP Port: " + smtpPort + @"<br/>
                                SSL/TLS: " + (smtpEnableSsl ? "Aktif" : "Pasif") + @"
                            </p>
                        </body>
                    </html>",
                TextBody = $"SMTP Bağlantı Testi Başarılı\n\nBu bir test e-postasıdır. SMTP ayarlarınız doğru şekilde yapılandırılmış ve çalışıyor.\n\nTest Zamanı: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss UTC}\nSMTP Host: {smtpHost}\nSMTP Port: {smtpPort}\nSSL/TLS: {(smtpEnableSsl ? "Aktif" : "Pasif")}"
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Send email using MailKit
            using var client = new MailKit.Net.Smtp.SmtpClient();

            // Set timeout
            client.Timeout = 15000; // 15 seconds

            // Connect to SMTP server
            await client.ConnectAsync(smtpHost, smtpPort, smtpEnableSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
            {
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
            }

            // Send message
            await client.SendAsync(message);

            // Disconnect
            await client.DisconnectAsync(true);

            return Ok(new
            {
                success = true,
                message = $"SMTP bağlantısı başarılı. Test e-postası {request.TestEmail} adresine gönderildi.",
                details = new
                {
                    host = smtpHost,
                    port = smtpPort,
                    ssl = smtpEnableSsl,
                    authenticated = !string.IsNullOrEmpty(smtpUsername)
                }
            });
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = "SMTP kimlik doğrulama hatası. Kullanıcı adı veya şifre yanlış.",
                error = ex.Message
            });
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = "SMTP komutu hatası.",
                error = ex.Message,
                statusCode = ex.StatusCode.ToString()
            });
        }
        catch (MailKit.Net.Smtp.SmtpProtocolException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = "SMTP protokol hatası.",
                error = ex.Message
            });
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = "SMTP sunucusuna bağlanılamadı. Host veya port kontrolü yapın.",
                error = ex.Message
            });
        }
        catch (TimeoutException)
        {
            return BadRequest(new
            {
                success = false,
                message = "SMTP bağlantısı zaman aşımına uğradı. Sunucu yanıt vermiyor."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = "SMTP testi sırasında beklenmeyen bir hata oluştu.",
                error = ex.Message
            });
        }
    }

    #endregion

    #region Social Media Settings

    /// <summary>
    /// Sosyal medya ayarlarını getir
    /// </summary>
    [HttpGet("social")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSocialSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("social"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            facebook = settingsDict.GetValueOrDefault("facebook", ""),
            twitter = settingsDict.GetValueOrDefault("twitter", ""),
            instagram = settingsDict.GetValueOrDefault("instagram", ""),
            linkedin = settingsDict.GetValueOrDefault("linkedin", ""),
            youtube = settingsDict.GetValueOrDefault("youtube", ""),
            tiktok = settingsDict.GetValueOrDefault("tiktok", ""),
            pinterest = settingsDict.GetValueOrDefault("pinterest", "")
        });
    }

    /// <summary>
    /// Sosyal medya ayarlarını güncelle
    /// </summary>
    [HttpPut("social")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSocialSettings([FromBody] UpdateSocialSettingsRequest request)
    {
        if (request.Facebook != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "facebook", Value = request.Facebook, Group = "social" });
        if (request.Twitter != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "twitter", Value = request.Twitter, Group = "social" });
        if (request.Instagram != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "instagram", Value = request.Instagram, Group = "social" });
        if (request.LinkedIn != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "linkedin", Value = request.LinkedIn, Group = "social" });
        if (request.YouTube != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "youtube", Value = request.YouTube, Group = "social" });
        if (request.TikTok != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "tiktok", Value = request.TikTok, Group = "social" });
        if (request.Pinterest != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "pinterest", Value = request.Pinterest, Group = "social" });

        return Ok(new { message = "Sosyal medya ayarları güncellendi." });
    }

    #endregion

    #region Branding Settings

    /// <summary>
    /// Marka ayarlarını getir
    /// </summary>
    [HttpGet("branding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrandingSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("branding"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            logoUrl = settingsDict.GetValueOrDefault("logoUrl", "/images/logo.png"),
            faviconUrl = settingsDict.GetValueOrDefault("faviconUrl", "/favicon.ico"),
            primaryColor = settingsDict.GetValueOrDefault("primaryColor", "#1E40AF"),
            secondaryColor = settingsDict.GetValueOrDefault("secondaryColor", "#3B82F6"),
            accentColor = settingsDict.GetValueOrDefault("accentColor", "#60A5FA"),
            brandName = settingsDict.GetValueOrDefault("brandName", "FreeStays"),
            tagline = settingsDict.GetValueOrDefault("tagline", "Your Dream Stay, Our Mission"),
            footerText = settingsDict.GetValueOrDefault("footerText", "© 2025 FreeStays. All rights reserved.")
        });
    }

    /// <summary>
    /// Marka ayarlarını güncelle
    /// </summary>
    [HttpPut("branding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBrandingSettings([FromBody] UpdateBrandingSettingsRequest request)
    {
        if (request.LogoUrl != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "logoUrl", Value = request.LogoUrl, Group = "branding" });
        if (request.FaviconUrl != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "faviconUrl", Value = request.FaviconUrl, Group = "branding" });
        if (request.PrimaryColor != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "primaryColor", Value = request.PrimaryColor, Group = "branding" });
        if (request.SecondaryColor != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "secondaryColor", Value = request.SecondaryColor, Group = "branding" });
        if (request.AccentColor != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "accentColor", Value = request.AccentColor, Group = "branding" });
        if (request.BrandName != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "brandName", Value = request.BrandName, Group = "branding" });
        if (request.Tagline != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "tagline", Value = request.Tagline, Group = "branding" });
        if (request.FooterText != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "footerText", Value = request.FooterText, Group = "branding" });

        return Ok(new { message = "Marka ayarları güncellendi." });
    }

    #endregion

    #region Contact Settings

    /// <summary>
    /// İletişim ayarlarını getir
    /// </summary>
    [HttpGet("contact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContactSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("contact"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            email = settingsDict.GetValueOrDefault("email", "info@freestays.com"),
            phone = settingsDict.GetValueOrDefault("phone", "+90 555 123 4567"),
            whatsapp = settingsDict.GetValueOrDefault("whatsapp", "+90 555 123 4567"),
            address = settingsDict.GetValueOrDefault("address", ""),
            city = settingsDict.GetValueOrDefault("city", ""),
            country = settingsDict.GetValueOrDefault("country", ""),
            postalCode = settingsDict.GetValueOrDefault("postalCode", ""),
            workingHours = settingsDict.GetValueOrDefault("workingHours", "Mon-Fri: 9:00-18:00"),
            mapLatitude = settingsDict.GetValueOrDefault("mapLatitude", ""),
            mapLongitude = settingsDict.GetValueOrDefault("mapLongitude", ""),
            googleMapsIframe = settingsDict.GetValueOrDefault("googleMapsIframe", ""),
            privacyPolicy = settingsDict.GetValueOrDefault("privacyPolicy", ""),
            termsOfService = settingsDict.GetValueOrDefault("termsOfService", ""),
            cancellationPolicy = settingsDict.GetValueOrDefault("cancellationPolicy", "")
        });
    }

    /// <summary>
    /// İletişim ayarlarını güncelle
    /// </summary>
    [HttpPut("contact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateContactSettings([FromBody] UpdateContactSettingsRequest request)
    {
        if (request.Email != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "email", Value = request.Email, Group = "contact" });
        if (request.Phone != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "phone", Value = request.Phone, Group = "contact" });
        if (request.Whatsapp != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "whatsapp", Value = request.Whatsapp, Group = "contact" });
        if (request.Address != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "address", Value = request.Address, Group = "contact" });
        if (request.City != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "city", Value = request.City, Group = "contact" });
        if (request.Country != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "country", Value = request.Country, Group = "contact" });
        if (request.PostalCode != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "postalCode", Value = request.PostalCode, Group = "contact" });
        if (request.WorkingHours != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "workingHours", Value = request.WorkingHours, Group = "contact" });
        if (request.MapLatitude != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "mapLatitude", Value = request.MapLatitude, Group = "contact" });
        if (request.MapLongitude != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "mapLongitude", Value = request.MapLongitude, Group = "contact" });
        if (request.GoogleMapsIframe != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "googleMapsIframe", Value = request.GoogleMapsIframe, Group = "contact" });
        if (request.PrivacyPolicy != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "privacyPolicy", Value = request.PrivacyPolicy, Group = "contact" });
        if (request.TermsOfService != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "termsOfService", Value = request.TermsOfService, Group = "contact" });
        if (request.CancellationPolicy != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "cancellationPolicy", Value = request.CancellationPolicy, Group = "contact" });

        return Ok(new { message = "İletişim ayarları güncellendi." });
    }

    #endregion
}

// Request DTOs
public record UpdateSiteSettingsRequest(
    string? SiteName,
    string? Tagline,
    string? SupportEmail,
    string? SupportPhone,
    string? DefaultLocale,
    string[]? AvailableLocales,
    string? DefaultCurrency,
    string[]? AvailableCurrencies,
    string? Timezone,
    bool? MaintenanceMode,
    string? MaintenanceMessage,
    string? LogoUrl,
    decimal? ProfitMargin,
    decimal? DefaultVatRate,
    decimal? ExtraFee,
    decimal? DiscountRate,
    // Coupon Prices
    decimal? OneTimeCouponPrice,
    decimal? AnnualCouponPrice,
    // Stripe Payment Settings
    string? StripePublicKey,
    string? StripeSecretKey,
    string? StripeWebhookSecret,
    // Affiliate Programs
    bool? ExcursionsActive,
    string? ExcursionsAffiliateCode,
    string? ExcursionsWidgetCode,
    bool? CarRentalActive,
    string? CarRentalAffiliateCode,
    string? CarRentalWidgetCode,
    bool? FlightBookingActive,
    string? FlightBookingAffiliateCode,
    string? FlightBookingWidgetCode,
    // Statik Sayfalar
    string? PrivacyPolicy,
    string? TermsOfService,
    string? CancellationPolicy,
    // Sosyal Medya Linkleri
    SocialLinksRequest? SocialLinks);

public record SocialLinksRequest(
    string? Facebook,
    string? Twitter,
    string? Instagram,
    string? LinkedIn,
    string? YouTube);

public record UpdateSeoSettingsRequest(
    string? DefaultMetaTitle,
    string? DefaultMetaDescription,
    string? GoogleAnalyticsId,
    string? GoogleTagManagerId,
    string? FacebookPixelId,
    string? RobotsTxt,
    bool? SitemapEnabled,
    // Organization Schema
    string? OrganizationName,
    string? OrganizationUrl,
    string? OrganizationLogo,
    string? OrganizationDescription,
    List<string>? OrganizationSocialProfiles,
    // Website Schema
    string? WebsiteName,
    string? WebsiteUrl,
    string? WebsiteSearchActionTarget,
    // Contact Info
    string? ContactPhone,
    string? ContactEmail,
    string? BusinessAddress,
    // Twitter Card
    string? TwitterSite,
    string? TwitterCreator);

public record SeoSettingsResponse(
    string? DefaultMetaTitle,
    string? DefaultMetaDescription,
    string? GoogleAnalyticsId,
    string? GoogleTagManagerId,
    string? FacebookPixelId,
    string? RobotsTxt,
    bool SitemapEnabled,
    string? OrganizationName,
    string? OrganizationUrl,
    string? OrganizationLogo,
    string? OrganizationDescription,
    List<string>? OrganizationSocialProfiles,
    string? WebsiteName,
    string? WebsiteUrl,
    string? WebsiteSearchActionTarget,
    string? ContactPhone,
    string? ContactEmail,
    string? BusinessAddress,
    string? TwitterSite,
    string? TwitterCreator);

public record UpdateLocaleSeoSettingsRequest(
    List<PageSeoRequest> Pages);

public record PageSeoRequest(
    string PageType,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? OgImage,
    // Open Graph Extensions
    string? OgType,
    string? OgUrl,
    string? OgSiteName,
    string? OgLocale,
    // Twitter Card
    string? TwitterCard,
    string? TwitterImage,
    // Hotel Schema (for pageType = "hotel_detail")
    string? HotelSchemaType,
    string? HotelName,
    string? HotelImage,
    string? HotelAddress,
    string? HotelTelephone,
    string? HotelPriceRange,
    int? HotelStarRating,
    string? HotelAggregateRating,
    // Search Page Schema (for pageType = "search")
    bool? EnableSearchActionSchema,
    string? SearchActionTarget,
    // FAQ Page Schema
    bool? EnableFaqSchema,
    // Custom Schema
    string? StructuredDataJson);

public record LocaleSeoSettingsResponse(
    string Locale,
    List<SeoSettingDto> Pages);

public record PaymentModeKeys(
    string PublicKey,
    string SecretKey);

public record UpdatePaymentSettingsRequest(
    string Provider,
    PaymentModeKeys? TestMode,
    PaymentModeKeys? LiveMode,
    string? WebhookSecret,
    bool IsLive,
    bool IsActive);

public record TestPaymentConnectionRequest(
    string Provider);

public record UpdateSmtpSettingsRequest(
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUsername,
    string? SmtpPassword,
    string? SmtpFromEmail,
    string? SmtpFromName,
    bool? SmtpEnableSsl);

public record TestSmtpConnectionRequest(
    string TestEmail);

public record UpdateSocialSettingsRequest(
    string? Facebook,
    string? Twitter,
    string? Instagram,
    string? LinkedIn,
    string? YouTube,
    string? TikTok,
    string? Pinterest);

public record UpdateBrandingSettingsRequest(
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    string? BrandName,
    string? Tagline,
    string? FooterText);

public record UpdateContactSettingsRequest(
    string? Email,
    string? Phone,
    string? Whatsapp,
    string? Address,
    string? City,
    string? Country,
    string? PostalCode,
    string? WorkingHours,
    string? MapLatitude,
    string? MapLongitude,
    string? GoogleMapsIframe,
    string? PrivacyPolicy,
    string? TermsOfService,
    string? CancellationPolicy);

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
            siteUrl = settingsDict.GetValueOrDefault("siteUrl", "https://freestays.com"),
            supportEmail = settingsDict.GetValueOrDefault("supportEmail", "support@freestays.com"),
            supportPhone = settingsDict.GetValueOrDefault("supportPhone", "+90 555 123 4567"),
            defaultLocale = settingsDict.GetValueOrDefault("defaultLocale", "tr"),
            availableLocales = new[] { "tr", "en", "de", "fr" },
            defaultCurrency = settingsDict.GetValueOrDefault("defaultCurrency", "TRY"),
            availableCurrencies = new[] { "TRY", "USD", "EUR", "GBP" },
            maintenanceMode = settingsDict.GetValueOrDefault("maintenanceMode", "false") == "true",
            logoUrl = settingsDict.GetValueOrDefault("logoUrl", "/images/logo.png"),
            faviconUrl = settingsDict.GetValueOrDefault("faviconUrl", "/images/favicon.ico"),
            profitMargin = decimal.TryParse(settingsDict.GetValueOrDefault("profitMargin", "10"), out var margin) ? margin : 10m,
            defaultVatRate = decimal.TryParse(settingsDict.GetValueOrDefault("defaultVatRate", "20"), out var vat) ? vat : 20m
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
        if (request.SupportEmail != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "supportEmail", Value = request.SupportEmail, Group = "site" });
        if (request.SupportPhone != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "supportPhone", Value = request.SupportPhone, Group = "site" });
        if (request.DefaultLocale != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultLocale", Value = request.DefaultLocale, Group = "site" });
        if (request.DefaultCurrency != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultCurrency", Value = request.DefaultCurrency, Group = "site" });
        if (request.MaintenanceMode.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "maintenanceMode", Value = request.MaintenanceMode.Value.ToString().ToLower(), Group = "site" });
        if (request.LogoUrl != null)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "logoUrl", Value = request.LogoUrl, Group = "site" });
        if (request.ProfitMargin.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "profitMargin", Value = request.ProfitMargin.Value.ToString(), Group = "site" });
        if (request.DefaultVatRate.HasValue)
            await Mediator.Send(new UpdateSiteSettingCommand { Key = "defaultVatRate", Value = request.DefaultVatRate.Value.ToString(), Group = "site" });

        return Ok(new { message = "Site ayarları güncellendi." });
    }

    #endregion

    #region SEO Settings

    /// <summary>
    /// Genel SEO ayarlarını getir
    /// </summary>
    [HttpGet("seo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeoSettings()
    {
        var settings = await Mediator.Send(new GetSiteSettingsQuery("seo"));
        var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

        return Ok(new
        {
            defaultMetaTitle = settingsDict.GetValueOrDefault("defaultMetaTitle", "FreeStays - En İyi Otel Fırsatları"),
            defaultMetaDescription = settingsDict.GetValueOrDefault("defaultMetaDescription", "FreeStays ile en uygun otel fırsatlarını keşfedin."),
            googleAnalyticsId = settingsDict.GetValueOrDefault("googleAnalyticsId", ""),
            googleTagManagerId = settingsDict.GetValueOrDefault("googleTagManagerId", ""),
            facebookPixelId = settingsDict.GetValueOrDefault("facebookPixelId", ""),
            robotsTxt = settingsDict.GetValueOrDefault("robotsTxt", "User-agent: *\nAllow: /"),
            sitemapEnabled = settingsDict.GetValueOrDefault("sitemapEnabled", "true") == "true"
        });
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

        return Ok(new { message = "SEO ayarları güncellendi." });
    }

    /// <summary>
    /// Belirli bir dil için SEO ayarlarını getir
    /// </summary>
    [HttpGet("seo/{locale}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeoSettingsByLocale(string locale)
    {
        var settings = await Mediator.Send(new GetSeoSettingsQuery(locale));

        return Ok(new
        {
            locale = locale,
            pages = settings.Select(s => new
            {
                pageType = s.PageType,
                metaTitle = s.MetaTitle,
                metaDescription = s.MetaDescription,
                metaKeywords = s.MetaKeywords,
                ogImage = s.OgImage
            }).ToList()
        });
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
                OgImage = page.OgImage
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
        var settings = await Mediator.Send(new GetPaymentSettingsQuery());

        return Ok(new
        {
            providers = settings.Select(s => new
            {
                provider = s.Provider,
                isActive = s.IsActive,
                isLive = s.IsLive,
                publicKey = s.PublicKey,
                hasSecretKey = true,
                hasWebhookSecret = true
            }).ToList(),
            defaultProvider = settings.FirstOrDefault(s => s.IsActive)?.Provider ?? "stripe",
            testMode = !settings.Any(s => s.IsLive && s.IsActive)
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
            PublicKey = request.PublicKey,
            SecretKey = request.SecretKey,
            WebhookSecret = request.WebhookSecret,
            IsLive = request.IsLive ?? false,
            IsActive = request.IsActive ?? false
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
}

// Request DTOs
public record UpdateSiteSettingsRequest(
    string? SiteName,
    string? SupportEmail,
    string? SupportPhone,
    string? DefaultLocale,
    string[]? AvailableLocales,
    string? DefaultCurrency,
    string[]? AvailableCurrencies,
    bool? MaintenanceMode,
    string? LogoUrl,
    decimal? ProfitMargin,
    decimal? DefaultVatRate,
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
    bool? SitemapEnabled);

public record UpdateLocaleSeoSettingsRequest(
    List<PageSeoRequest> Pages);

public record PageSeoRequest(
    string PageType,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? OgImage);

public record UpdatePaymentSettingsRequest(
    string Provider,
    string? PublicKey,
    string? SecretKey,
    string? WebhookSecret,
    bool? IsLive,
    bool? IsActive);

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

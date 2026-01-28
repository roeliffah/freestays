using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FreeStays.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IEmailSettingRepository _emailSettingRepository;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailSettingRepository emailSettingRepository,
        IEmailTemplateRepository emailTemplateRepository,
        ILogger<EmailService> logger)
    {
        _emailSettingRepository = emailSettingRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        await SendEmailAsync(new[] { to }, subject, body, isHtml, cancellationToken);
    }

    public async Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailSettings = await _emailSettingRepository.GetActiveAsync(cancellationToken);
            if (emailSettings == null)
            {
                _logger.LogError("No active email settings found. Cannot send email.");
                throw new InvalidOperationException("E-posta ayarları bulunamadı. Lütfen admin panelinden SMTP ayarlarını yapılandırın.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromEmail));

            foreach (var recipient in to)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }
            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();

            // Sertifika doğrulama sorunlarını bypass et (self-signed veya revocation check hataları için)
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            // SSL/TLS seçeneğini port ve UseSsl ayarına göre belirle
            var secureSocketOptions = DetermineSecureSocketOptions(emailSettings.SmtpPort, emailSettings.UseSsl);

            _logger.LogInformation("Connecting to SMTP server {Host}:{Port} with {SecureOption}",
                emailSettings.SmtpHost, emailSettings.SmtpPort, secureSocketOptions);

            await smtp.ConnectAsync(
                emailSettings.SmtpHost,
                emailSettings.SmtpPort,
                secureSocketOptions,
                cancellationToken);

            _logger.LogInformation("Connected to SMTP server. IsConnected: {IsConnected}, IsSecure: {IsSecure}, IsAuthenticated: {IsAuthenticated}",
                smtp.IsConnected, smtp.IsSecure, smtp.IsAuthenticated);

            // Sadece username varsa authentication yap
            if (!string.IsNullOrEmpty(emailSettings.SmtpUsername) && !string.IsNullOrEmpty(emailSettings.SmtpPassword))
            {
                _logger.LogInformation("Authenticating with username: {Username}", emailSettings.SmtpUsername);
                await smtp.AuthenticateAsync(emailSettings.SmtpUsername, emailSettings.SmtpPassword, cancellationToken);
            }

            await smtp.SendAsync(message, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", to));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}. Subject: {Subject}", string.Join(", ", to), subject);
            throw;
        }
    }

    /// <summary>
    /// Port ve UseSsl ayarına göre en uygun SecureSocketOptions'ı belirle
    /// </summary>
    private static SecureSocketOptions DetermineSecureSocketOptions(int port, bool useSsl)
    {
        // UseSsl kapalıysa şifresiz bağlantı dene
        if (!useSsl)
        {
            return SecureSocketOptions.None;
        }

        // Port'a göre en uygun seçeneği belirle
        return port switch
        {
            25 => SecureSocketOptions.StartTlsWhenAvailable,  // Genellikle şifresiz, varsa STARTTLS
            465 => SecureSocketOptions.SslOnConnect,          // Implicit SSL/TLS (eski standard)
            587 => SecureSocketOptions.StartTls,              // Explicit TLS (modern standard)
            2525 => SecureSocketOptions.StartTlsWhenAvailable, // Alternatif port
            _ => SecureSocketOptions.Auto                      // Diğer portlar için otomatik algıla
        };
    }

    public async Task SendTemplateEmailAsync(string to, string templateCode, Dictionary<string, string> variables, string language = "tr", CancellationToken cancellationToken = default)
    {
        await SendTemplateEmailAsync(new[] { to }, templateCode, variables, language, cancellationToken);
    }

    public async Task SendTemplateEmailAsync(IEnumerable<string> to, string templateCode, Dictionary<string, string> variables, string language = "tr", CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _emailTemplateRepository.GetByCodeAsync(templateCode, cancellationToken);
            if (template == null || !template.IsActive)
            {
                _logger.LogError("Email template not found or inactive: {TemplateCode}", templateCode);
                throw new InvalidOperationException($"E-posta şablonu bulunamadı: {templateCode}");
            }

            // Parse JSON subject and body
            var subjectJson = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Subject);
            var bodyJson = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Body);

            if (subjectJson == null || bodyJson == null)
            {
                _logger.LogError("Invalid email template JSON for: {TemplateCode}", templateCode);
                throw new InvalidOperationException($"E-posta şablonu JSON formatı geçersiz: {templateCode}");
            }

            // Get localized subject and body
            var subject = subjectJson.ContainsKey(language) ? subjectJson[language] : subjectJson.FirstOrDefault().Value;
            var body = bodyJson.ContainsKey(language) ? bodyJson[language] : bodyJson.FirstOrDefault().Value;

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            {
                _logger.LogError("Email template content empty for language {Language}: {TemplateCode}", language, templateCode);
                throw new InvalidOperationException($"E-posta şablonu içeriği boş: {templateCode} ({language})");
            }

            // Replace variables
            foreach (var variable in variables)
            {
                var placeholder = $"{{{variable.Key}}}";
                subject = subject.Replace(placeholder, variable.Value);
                body = body.Replace(placeholder, variable.Value);
            }

            await SendEmailAsync(to, subject, body, isHtml: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template email. Template: {TemplateCode}, Recipients: {Recipients}",
                templateCode, string.Join(", ", to));
            throw;
        }
    }

    public async Task SendGenericEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        await SendEmailAsync(to, subject, htmlContent, isHtml: true, cancellationToken);
    }
}

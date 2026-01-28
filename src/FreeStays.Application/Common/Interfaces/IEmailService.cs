namespace FreeStays.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// E-posta gönderir
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla alıcıya e-posta gönderir
    /// </summary>
    Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// E-posta template kullanarak gönderir
    /// </summary>
    Task SendTemplateEmailAsync(string to, string templateCode, Dictionary<string, string> variables, string language = "tr", CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla alıcıya template e-posta gönderir
    /// </summary>
    Task SendTemplateEmailAsync(IEnumerable<string> to, string templateCode, Dictionary<string, string> variables, string language = "tr", CancellationToken cancellationToken = default);

    /// <summary>
    /// Genel amaçlı HTML e-posta gönderir (template olmadan)
    /// After-sale follow-up emailleri için kullanılır
    /// </summary>
    Task SendGenericEmailAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default);
}

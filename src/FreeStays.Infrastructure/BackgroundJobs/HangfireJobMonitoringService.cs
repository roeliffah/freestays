using System.Net.Mail;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FreeStays.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire Job Monitoring ve Failure Notification Servisi
/// Failed job'lar hakkında admin'e email gönderir
/// SMTP bilgileri veritabanından yüklenir
/// </summary>
public interface IHangfireJobMonitoringService
{
    /// <summary>
    /// Failed job'lar için monitoring ve email bildirim başlat
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir job'un fail olması durumunda email gönder
    /// </summary>
    Task HandleJobFailureAsync(string jobId, string jobName, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Failed job'ları ve retry sayılarını kontrol et
    /// </summary>
    Task CheckFailedJobsAsync(CancellationToken cancellationToken = default);
}

public class HangfireJobMonitoringService : IHangfireJobMonitoringService
{
    private readonly FreeStaysDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HangfireJobMonitoringService> _logger;
    private readonly string? _adminEmail;
    private CancellationTokenSource? _monitoringCts;

    public HangfireJobMonitoringService(
        FreeStaysDbContext dbContext,
        IConfiguration configuration,
        ILogger<HangfireJobMonitoringService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _adminEmail = _configuration["Hangfire:AdminEmail"];
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_adminEmail))
        {
            _logger.LogWarning("Hangfire admin email not configured. Monitoring disabled.");
            return;
        }

        _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Her 5 dakikada bir check et
        _ = Task.Run(async () =>
        {
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    await CheckFailedJobsAsync(_monitoringCts.Token);
                    await Task.Delay(TimeSpan.FromMinutes(5), _monitoringCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Hangfire job monitoring");
                }
            }
        }, _monitoringCts.Token);

        _logger.LogInformation("✅ Hangfire job monitoring started. Admin notifications will be sent to: {Email}", _adminEmail);
    }

    public async Task HandleJobFailureAsync(
        string jobId,
        string jobName,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception, "❌ Hangfire job failed - JobId: {JobId}, JobName: {JobName}", jobId, jobName);

        try
        {
            var subject = $"🚨 Hangfire Job Failed: {jobName}";
            var body = BuildFailureEmailBody(jobId, jobName, exception);

            await SendEmailAsync(subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send job failure notification email");
        }
    }

    public async Task CheckFailedJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Hangfire job statistics almak için Monitor API kullanıyoruz
            var monitor = JobStorage.Current.GetMonitoringApi();
            var stats = monitor.GetStatistics();

            if (stats.Failed > 0)
            {
                _logger.LogWarning("⚠️ {Count} failed jobs detected in Hangfire", stats.Failed);

                if (stats.Failed > 10) // 10'dan fazla failed job varsa alert gönder
                {
                    var subject = "🚨 Hangfire Alert: Multiple Failed Jobs";
                    var body = BuildAlertEmailBody((int)stats.Failed);

                    await SendEmailAsync(subject, body, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking failed Hangfire jobs");
        }
    }

    private async Task SendEmailAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_adminEmail))
        {
            _logger.LogWarning("Admin email not configured. Email not sent.");
            return;
        }

        try
        {
            // ✅ Veritabanından SMTP ayarlarını al
            var emailSetting = await _dbContext.EmailSettings
                .Where(es => es.IsActive && !string.IsNullOrEmpty(es.SmtpHost))
                .FirstOrDefaultAsync(cancellationToken);

            if (emailSetting == null)
            {
                _logger.LogWarning("⚠️ No active email settings configured in database. Email not sent.");
                return;
            }

            using var client = new SmtpClient(emailSetting.SmtpHost, emailSetting.SmtpPort)
            {
                Credentials = new System.Net.NetworkCredential(emailSetting.SmtpUsername, emailSetting.SmtpPassword),
                EnableSsl = emailSetting.UseSsl
            };

            var message = new MailMessage(emailSetting.FromEmail, _adminEmail, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("✅ Admin notification email sent to {AdminEmail} via {SmtpHost}", _adminEmail, emailSetting.SmtpHost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification to admin");
            throw;
        }
    }

    private string BuildFailureEmailBody(string jobId, string jobName, Exception exception)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        var errorMessage = exception?.Message ?? "Unknown error";
        var stackTrace = exception?.StackTrace ?? "No stack trace available";

        return $@"
            <h2 style='color: #d32f2f;'>❌ Hangfire Job Failed</h2>
            <table style='border-collapse: collapse; width: 100%;'>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Job ID:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(jobId)}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Job Name:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(jobName)}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Timestamp:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{timestamp}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold; vertical-align: top;'>Error Message:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'><code>{System.Net.WebUtility.HtmlEncode(errorMessage)}</code></td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold; vertical-align: top;'>Stack Trace:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'><pre style='background: #f5f5f5; overflow-x: auto;'>{System.Net.WebUtility.HtmlEncode(stackTrace)}</pre></td>
                </tr>
            </table>
            <p style='margin-top: 20px; color: #666;'>⏭️ Action Required: Check Hangfire Dashboard for retry options</p>
        ";
    }

    private string BuildAlertEmailBody(int failedCount)
    {
        var timestamp = DateTime.UtcNow.ToString("o");

        return $@"
            <h2 style='color: #ff9800;'>⚠️ Hangfire Alert: Multiple Failed Jobs</h2>
            <p>System detected <strong>{failedCount} failed jobs</strong> in Hangfire queue.</p>
            <table style='border-collapse: collapse; width: 100%;'>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Failed Job Count:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'><strong>{failedCount}</strong></td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>Timestamp:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{timestamp}</td>
                </tr>
            </table>
            <p style='margin-top: 20px; color: #d32f2f; font-weight: bold;'>🔴 Immediate Action Required!</p>
            <p>Please review the Hangfire Dashboard and investigate the root causes.</p>
        ";
    }
}

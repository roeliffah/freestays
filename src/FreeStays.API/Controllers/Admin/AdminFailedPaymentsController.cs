using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Entities;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers.Admin;

/// <summary>
/// After-Sale: Başarısız ödemelerin yönetimi
/// Python: failed_payments collection karşılığı
/// </summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[ApiController]
[Route("api/v1/admin/failed-payments")]
[Produces("application/json")]
public class AdminFailedPaymentsController : ControllerBase
{
    private readonly FreeStaysDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminFailedPaymentsController> _logger;

    public AdminFailedPaymentsController(
        FreeStaysDbContext dbContext,
        IEmailService emailService,
        ILogger<AdminFailedPaymentsController> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm başarısız ödemeleri listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? failureType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FailedPayments.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(fp => fp.Status == status);
        }

        if (!string.IsNullOrEmpty(failureType))
        {
            query = query.Where(fp => fp.FailureType == failureType);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(fp => fp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(fp => new FailedPaymentDto
            {
                Id = fp.Id,
                SessionId = fp.SessionId,
                BookingId = fp.BookingId,
                CustomerEmail = fp.CustomerEmail,
                CustomerName = fp.CustomerName,
                FailureType = fp.FailureType,
                Amount = fp.Amount,
                Currency = fp.Currency,
                HotelName = fp.HotelName,
                CheckIn = fp.CheckIn,
                CheckOut = fp.CheckOut,
                Status = fp.Status,
                ContactReason = fp.ContactReason,
                ContactedAt = fp.ContactedAt,
                Notes = fp.Notes,
                CreatedAt = fp.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// İstatistikleri getirir
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _dbContext.FailedPayments
            .GroupBy(fp => fp.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var typeStats = await _dbContext.FailedPayments
            .GroupBy(fp => fp.FailureType)
            .Select(g => new { FailureType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalAmount = await _dbContext.FailedPayments
            .Where(fp => fp.Status == "pending")
            .SumAsync(fp => fp.Amount, cancellationToken);

        return Ok(new
        {
            byStatus = stats,
            byFailureType = typeStats,
            pendingTotalAmount = totalAmount,
            pendingCount = stats.FirstOrDefault(s => s.Status == "pending")?.Count ?? 0,
            contactedCount = stats.FirstOrDefault(s => s.Status == "contacted")?.Count ?? 0,
            resolvedCount = stats.FirstOrDefault(s => s.Status == "resolved")?.Count ?? 0
        });
    }

    /// <summary>
    /// Başarısız ödeme detayını getirir
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var fp = await _dbContext.FailedPayments
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (fp == null)
        {
            return NotFound(new { message = "Kayıt bulunamadı" });
        }

        return Ok(fp);
    }

    /// <summary>
    /// Manuel follow-up email gönderir
    /// </summary>
    [HttpPost("{id:guid}/send-email")]
    public async Task<IActionResult> SendFollowUpEmail(
        Guid id,
        [FromBody] SendFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        var fp = await _dbContext.FailedPayments.FindAsync([id], cancellationToken);
        if (fp == null)
        {
            return NotFound(new { message = "Kayıt bulunamadı" });
        }

        if (string.IsNullOrEmpty(fp.CustomerEmail))
        {
            return BadRequest(new { message = "Müşteri email adresi yok" });
        }

        try
        {
            // Email template'leri al
            var settings = await _dbContext.SiteSettings
                .Where(s => s.Group == "aftersale")
                .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

            var templates = new Dictionary<string, string>
            {
                { "no_payment", settings.GetValueOrDefault("aftersale_email_no_payment", "Ödemenizin tamamlanmadığını fark ettik. Rezervasyonunuzu tamamlamak ister misiniz?") },
                { "stop_payment", settings.GetValueOrDefault("aftersale_email_stop_payment", "Ödemenizi durdurduğunuzu anlıyoruz. Size yardımcı olabileceğimiz bir konu var mı?") },
                { "not_interested", settings.GetValueOrDefault("aftersale_email_not_interested", "Gittiğinizi görmek bizi üzdü. İyileştirebileceğimiz konularda geri bildirimde bulunmak ister misiniz?") },
                { "new_offers", settings.GetValueOrDefault("aftersale_email_new_offers", "İlginizi çekebilecek harika yeni tekliflerimiz var! En son fırsatlarımıza göz atın.") }
            };

            var reason = request.Reason ?? "no_payment";
            var template = templates.GetValueOrDefault(reason, templates["no_payment"]);
            var customerFirstName = fp.CustomerName?.Split(' ').FirstOrDefault() ?? "Değerli Müşterimiz";

            // Custom message varsa onu kullan
            var messageContent = !string.IsNullOrEmpty(request.CustomMessage)
                ? request.CustomMessage
                : template;

            var htmlContent = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); padding: 30px; text-align: center;'>
        <h1 style='color: white; margin: 0;'>FreeStays</h1>
    </div>
    <div style='padding: 30px; background: #f9f9f9;'>
        <p>Sayın {fp.CustomerName ?? "Değerli Müşterimiz"},</p>
        <p>{messageContent}</p>
        {(fp.HotelName != null ? $@"
        <p><strong>Rezervasyon detaylarınız:</strong><br/>
        Otel: {fp.HotelName}<br/>
        Giriş: {fp.CheckIn?.ToString("dd.MM.yyyy") ?? "N/A"}<br/>
        Çıkış: {fp.CheckOut?.ToString("dd.MM.yyyy") ?? "N/A"}</p>" : "")}
        <p style='margin-top: 20px;'>
            <a href='https://travelar.eu' style='background: #1e3a5f; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>FreeStays'i Ziyaret Et</a>
        </p>
        <p style='margin-top: 20px; color: #666;'>Saygılarımızla,<br/>FreeStays Ekibi</p>
    </div>
</div>";

            await _emailService.SendGenericEmailAsync(
                fp.CustomerEmail,
                $"FreeStays - Sizden haber almak istiyoruz, {customerFirstName}!",
                htmlContent,
                cancellationToken);

            // Kayıt güncelle
            fp.Status = "contacted";
            fp.ContactReason = reason;
            fp.ContactedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(request.Notes))
            {
                fp.Notes = (fp.Notes ?? "") + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.Notes}";
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Follow-up email sent to {Email} for FailedPayment {Id}", fp.CustomerEmail, id);

            return Ok(new { success = true, message = "Email gönderildi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send follow-up email for {Id}", id);
            return StatusCode(500, new { success = false, message = "Email gönderilemedi: " + ex.Message });
        }
    }

    /// <summary>
    /// Durumu günceller
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var fp = await _dbContext.FailedPayments.FindAsync([id], cancellationToken);
        if (fp == null)
        {
            return NotFound(new { message = "Kayıt bulunamadı" });
        }

        fp.Status = request.Status;
        if (!string.IsNullOrEmpty(request.Notes))
        {
            fp.Notes = (fp.Notes ?? "") + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.Notes}";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, message = "Durum güncellendi" });
    }

    /// <summary>
    /// AfterSale ayarlarını getirir
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetAfterSaleSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.SiteSettings
            .Where(s => s.Group == "aftersale")
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return Ok(new AfterSaleSettingsDto
        {
            AutoSend = bool.TryParse(settings.GetValueOrDefault("aftersale_auto_send", "false"), out var autoSend) && autoSend,
            EmailNoPayment = settings.GetValueOrDefault("aftersale_email_no_payment", "Ödemenizin tamamlanmadığını fark ettik. Rezervasyonunuzu tamamlamak ister misiniz?"),
            EmailStopPayment = settings.GetValueOrDefault("aftersale_email_stop_payment", "Ödemenizi durdurduğunuzu anlıyoruz. Size yardımcı olabileceğimiz bir konu var mı?"),
            EmailNotInterested = settings.GetValueOrDefault("aftersale_email_not_interested", "Gittiğinizi görmek bizi üzdü. İyileştirebileceğimiz konularda geri bildirimde bulunmak ister misiniz?"),
            EmailNewOffers = settings.GetValueOrDefault("aftersale_email_new_offers", "İlginizi çekebilecek harika yeni tekliflerimiz var! En son fırsatlarımıza göz atın.")
        });
    }

    /// <summary>
    /// AfterSale ayarlarını günceller
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateAfterSaleSettings(
        [FromBody] AfterSaleSettingsDto request,
        CancellationToken cancellationToken)
    {
        var settingsToUpdate = new Dictionary<string, string>
        {
            { "aftersale_auto_send", request.AutoSend.ToString().ToLower() },
            { "aftersale_email_no_payment", request.EmailNoPayment ?? "" },
            { "aftersale_email_stop_payment", request.EmailStopPayment ?? "" },
            { "aftersale_email_not_interested", request.EmailNotInterested ?? "" },
            { "aftersale_email_new_offers", request.EmailNewOffers ?? "" }
        };

        foreach (var (key, value) in settingsToUpdate)
        {
            var existing = await _dbContext.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == key && s.Group == "aftersale", cancellationToken);

            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                await _dbContext.SiteSettings.AddAsync(new SiteSetting
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Value = value,
                    Group = "aftersale"
                }, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, message = "Ayarlar güncellendi" });
    }
}

// DTOs
public record FailedPaymentDto
{
    public Guid Id { get; init; }
    public string SessionId { get; init; } = "";
    public Guid? BookingId { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }
    public string FailureType { get; init; } = "";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string? HotelName { get; init; }
    public DateTime? CheckIn { get; init; }
    public DateTime? CheckOut { get; init; }
    public string Status { get; init; } = "pending";
    public string? ContactReason { get; init; }
    public DateTime? ContactedAt { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SendFollowUpRequest
{
    public string? Reason { get; init; } // no_payment, stop_payment, not_interested, new_offers
    public string? CustomMessage { get; init; }
    public string? Notes { get; init; }
}

public record UpdateStatusRequest
{
    public string Status { get; init; } = "pending"; // pending, contacted, resolved, not_interested
    public string? Notes { get; init; }
}

public record AfterSaleSettingsDto
{
    public bool AutoSend { get; init; }
    public string? EmailNoPayment { get; init; }
    public string? EmailStopPayment { get; init; }
    public string? EmailNotInterested { get; init; }
    public string? EmailNewOffers { get; init; }
}

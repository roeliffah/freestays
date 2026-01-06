using FreeStays.Application.Features.Coupons.Commands;
using FreeStays.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : BaseApiController
{
    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook([FromBody] StripeWebhookRequest payload)
    {
        // Basit işleme: payment_intent.succeeded geldiğinde kupon oluştur
        if (!string.Equals(payload.Type, "payment_intent.succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { received = true });
        }

        var kind = ParseKind(payload.Data?.Object?.Metadata?.Kind);
        var email = payload.Data?.Object?.ReceiptEmail ?? payload.Data?.Object?.CustomerEmail;
        Guid? userId = null;
        if (Guid.TryParse(payload.Data?.Object?.Metadata?.UserId, out var parsed))
        {
            userId = parsed;
        }

        await Mediator.Send(new CreateCouponCommand
        {
            Code = string.Empty,
            Description = "Stripe ödeme sonrası kupon",
            Kind = kind,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 0,
            MinBookingAmount = null,
            MaxUses = null,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddYears(1),
            AssignedUserId = userId,
            AssignedEmail = email,
            StripePaymentIntentId = payload.Data?.Object?.Id
        });

        return Ok(new { success = true });
    }

    private static CouponKind ParseKind(string? kind)
    {
        return string.Equals(kind, "annual", StringComparison.OrdinalIgnoreCase)
            ? CouponKind.Annual
            : CouponKind.OneTime;
    }
}

public class StripeWebhookRequest
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public StripeWebhookData? Data { get; set; }
}

public class StripeWebhookData
{
    public StripeWebhookObject? Object { get; set; }
}

public class StripeWebhookObject
{
    public string? Id { get; set; }
    public string? ReceiptEmail { get; set; }
    public string? CustomerEmail { get; set; }
    public StripeWebhookMetadata? Metadata { get; set; }
}

public class StripeWebhookMetadata
{
    public string? Kind { get; set; }
    public string? UserId { get; set; }
}

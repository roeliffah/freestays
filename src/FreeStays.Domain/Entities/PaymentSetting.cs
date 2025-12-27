using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class PaymentSetting : BaseEntity
{
    public string Provider { get; set; } = string.Empty; // stripe, paypal
    public string? TestModePublicKey { get; set; }
    public string? TestModeSecretKey { get; set; } // Encrypted
    public string? LiveModePublicKey { get; set; }
    public string? LiveModeSecretKey { get; set; } // Encrypted
    public string? WebhookSecret { get; set; }
    public bool IsLive { get; set; }
    public bool IsActive { get; set; }
    public string Settings { get; set; } = "{}"; // JSON - for additional provider-specific settings
}

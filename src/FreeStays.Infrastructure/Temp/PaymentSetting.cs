using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class PaymentSetting
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = null!;

    public string? PublicKey { get; set; }

    public string? SecretKey { get; set; }

    public string? WebhookSecret { get; set; }

    public bool IsLive { get; set; }

    public bool IsActive { get; set; }

    public string Settings { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

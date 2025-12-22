using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class ExternalServiceConfig
{
    public Guid Id { get; set; }

    public string ServiceName { get; set; } = null!;

    public string BaseUrl { get; set; } = null!;

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool IsActive { get; set; }

    public string? Settings { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

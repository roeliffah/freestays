using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class ExternalServiceConfig : BaseEntity
{
    public string ServiceName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Settings { get; set; } // JSON format for additional settings
}

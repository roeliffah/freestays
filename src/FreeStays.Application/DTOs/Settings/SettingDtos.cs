namespace FreeStays.Application.DTOs.Settings;

public record SiteSettingDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
}

public record SiteSettingsGroupDto
{
    public string Group { get; init; } = string.Empty;
    public Dictionary<string, string> Settings { get; init; } = new();
}

public record UpdateSiteSettingDto
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public record SeoSettingDto
{
    public Guid Id { get; init; }
    public string Locale { get; init; } = string.Empty;
    public string PageType { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public string? OgImage { get; init; }
}

public record UpdateSeoSettingDto
{
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public string? OgImage { get; init; }
}

public record PaymentSettingDto
{
    public Guid Id { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? PublicKey { get; init; }
    public bool IsLive { get; init; }
    public bool IsActive { get; init; }
    public string? Settings { get; init; }
}

public record UpdatePaymentSettingDto
{
    public string? PublicKey { get; init; }
    public string? SecretKey { get; init; }
    public string? WebhookSecret { get; init; }
    public bool IsLive { get; init; }
    public bool IsActive { get; init; }
    public string? Settings { get; init; }
}

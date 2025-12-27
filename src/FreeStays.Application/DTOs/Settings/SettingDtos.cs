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

    // Basic Meta Tags
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }

    // Open Graph
    public string? OgImage { get; init; }
    public string? OgType { get; init; }
    public string? OgUrl { get; init; }
    public string? OgSiteName { get; init; }
    public string? OgLocale { get; init; }

    // Twitter Card
    public string? TwitterCard { get; init; }
    public string? TwitterImage { get; init; }
    public string? TwitterSite { get; init; }
    public string? TwitterCreator { get; init; }

    // Organization Schema
    public string? OrganizationName { get; init; }
    public string? OrganizationUrl { get; init; }
    public string? OrganizationLogo { get; init; }
    public string? OrganizationDescription { get; init; }
    public string? OrganizationSocialProfiles { get; init; }

    // Website Schema
    public string? WebsiteName { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? WebsiteSearchActionTarget { get; init; }

    // Contact Info
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public string? BusinessAddress { get; init; }

    // Hotel Schema
    public string? HotelSchemaType { get; init; }
    public string? HotelName { get; init; }
    public string? HotelImage { get; init; }
    public string? HotelAddress { get; init; }
    public string? HotelTelephone { get; init; }
    public string? HotelPriceRange { get; init; }
    public int? HotelStarRating { get; init; }
    public string? HotelAggregateRating { get; init; }

    // Search Page Schema
    public bool EnableSearchActionSchema { get; init; }
    public string? SearchActionTarget { get; init; }

    // FAQ Page Schema
    public bool EnableFaqSchema { get; init; }

    // Custom Structured Data
    public string? StructuredDataJson { get; init; }
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
    public string? TestModePublicKey { get; init; }
    public string? TestModeSecretKey { get; init; }
    public string? LiveModePublicKey { get; init; }
    public string? LiveModeSecretKey { get; init; }
    public string? WebhookSecret { get; init; }
    public bool IsLive { get; init; }
    public bool IsActive { get; init; }
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

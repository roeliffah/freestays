using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class SeoSetting : BaseEntity
{
    public string Locale { get; set; } = string.Empty;
    public string PageType { get; set; } = string.Empty; // home, search, hotel_detail, about, etc.

    // Basic Meta Tags
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    // Open Graph
    public string? OgImage { get; set; }
    public string? OgType { get; set; } // "website", "article", etc.
    public string? OgUrl { get; set; }
    public string? OgSiteName { get; set; }
    public string? OgLocale { get; set; } // "tr_TR", "en_US"

    // Twitter Card
    public string? TwitterCard { get; set; } // "summary", "summary_large_image"
    public string? TwitterImage { get; set; }
    public string? TwitterSite { get; set; } // @username
    public string? TwitterCreator { get; set; } // @username

    // Organization Schema (for general pages)
    public string? OrganizationName { get; set; }
    public string? OrganizationUrl { get; set; }
    public string? OrganizationLogo { get; set; }
    public string? OrganizationDescription { get; set; }
    public string? OrganizationSocialProfiles { get; set; } // JSON array of URLs

    // Website Schema
    public string? WebsiteName { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? WebsiteSearchActionTarget { get; set; } // e.g., "https://freestays.com/search?q={search_term_string}"

    // Contact Info (for LocalBusiness schema)
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? BusinessAddress { get; set; } // JSON: {"streetAddress": "...", "addressLocality": "...", "addressCountry": "TR"}

    // Hotel Detail Page Schema (when pageType = "hotel_detail")
    public string? HotelSchemaType { get; set; } // "Hotel", "Resort"
    public string? HotelName { get; set; } // Dynamic: {{hotelName}}
    public string? HotelImage { get; set; }
    public string? HotelAddress { get; set; } // JSON string
    public string? HotelTelephone { get; set; }
    public string? HotelPriceRange { get; set; } // e.g., "$$-$$$"
    public int? HotelStarRating { get; set; } // 1-5
    public string? HotelAggregateRating { get; set; } // JSON: {"ratingValue": 4.5, "reviewCount": 230}

    // Search Page Schema (when pageType = "search")
    public bool EnableSearchActionSchema { get; set; }
    public string? SearchActionTarget { get; set; } // e.g., "https://freestays.com/search?q={search_term}"

    // FAQ Page Schema (when pageType = "about" or faq)
    public bool EnableFaqSchema { get; set; }

    // Custom Structured Data (for flexibility)
    public string? StructuredDataJson { get; set; } // Custom schema.org JSON-LD code
}

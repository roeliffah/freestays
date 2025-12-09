using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class SeoSetting : BaseEntity
{
    public string Locale { get; set; } = string.Empty;
    public string PageType { get; set; } = string.Empty; // home, search, hotel_detail, about, etc.
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImage { get; set; }
}

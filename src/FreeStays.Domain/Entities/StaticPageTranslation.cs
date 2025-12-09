using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class StaticPageTranslation : BaseEntity
{
    public Guid PageId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    
    // Navigation
    public StaticPage Page { get; set; } = null!;
}

using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class StaticPage : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public ICollection<StaticPageTranslation> Translations { get; set; } = new List<StaticPageTranslation>();
}

using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

/// <summary>
/// Section başlık ve alt başlıklarının çoklu dil desteği
/// </summary>
public class HomePageSectionTranslation : BaseEntity
{
    public Guid SectionId { get; set; }
    public string Locale { get; set; } = string.Empty; // 'tr', 'en', 'de', 'fr', 'es', 'it', 'nl', 'ru', 'el'
    public string? Title { get; set; }
    public string? Subtitle { get; set; }

    // Navigation
    public virtual HomePageSection Section { get; set; } = null!;
}

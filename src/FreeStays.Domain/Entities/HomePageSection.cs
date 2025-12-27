using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

/// <summary>
/// Homepage'de gösterilecek section'ları saklar
/// </summary>
public class HomePageSection : BaseEntity
{
    public string SectionType { get; set; } = string.Empty; // 'hero', 'room-types', 'features', 'popular-hotels', etc.
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string Configuration { get; set; } = "{}"; // JSON format

    // Navigation properties
    public virtual ICollection<HomePageSectionTranslation> Translations { get; set; } = new List<HomePageSectionTranslation>();
    public virtual ICollection<HomePageSectionHotel> Hotels { get; set; } = new List<HomePageSectionHotel>();
    public virtual ICollection<HomePageSectionDestination> Destinations { get; set; } = new List<HomePageSectionDestination>();
}

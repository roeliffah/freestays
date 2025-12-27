using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

/// <summary>
/// Popular Destinations section'ında gösterilecek destinasyonlar
/// </summary>
public class HomePageSectionDestination : BaseEntity
{
    public Guid SectionId { get; set; }
    public string DestinationId { get; set; } = string.Empty; // SunHotels API'den gelen destination ID
    public int DisplayOrder { get; set; }

    // Navigation
    public virtual HomePageSection Section { get; set; } = null!;
}

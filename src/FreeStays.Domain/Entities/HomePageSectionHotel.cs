using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

/// <summary>
/// Popular Hotels ve Romantic Tours section'larında gösterilecek oteller
/// </summary>
public class HomePageSectionHotel : BaseEntity
{
    public Guid SectionId { get; set; }
    public string HotelId { get; set; } = string.Empty; // SunHotels API'den gelen hotel ID
    public int DisplayOrder { get; set; }

    // Navigation
    public virtual HomePageSection Section { get; set; } = null!;
}

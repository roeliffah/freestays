using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class HotelFacility : BaseEntity
{
    public Guid HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    
    // Navigation property
    public virtual Hotel Hotel { get; set; } = null!;
}

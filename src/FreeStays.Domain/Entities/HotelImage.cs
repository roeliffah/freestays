using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class HotelImage : BaseEntity
{
    public Guid HotelId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Caption { get; set; }
    
    // Navigation property
    public virtual Hotel Hotel { get; set; } = null!;
}

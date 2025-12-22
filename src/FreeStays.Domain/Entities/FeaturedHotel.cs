using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class FeaturedHotel : BaseEntity
{
    public Guid HotelId { get; set; }
    public int Priority { get; set; } = 999;
    public FeaturedContentStatus Status { get; set; } = FeaturedContentStatus.Active;
    public Season Season { get; set; } = Season.AllSeason;
    public HotelCategory? Category { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? CampaignName { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? CreatedBy { get; set; }
    
    // Navigation
    public Hotel? Hotel { get; set; }
}

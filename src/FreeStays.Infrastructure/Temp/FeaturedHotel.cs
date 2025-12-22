using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class FeaturedHotel
{
    public Guid Id { get; set; }

    public Guid HotelId { get; set; }

    public int Priority { get; set; }

    public string Status { get; set; } = null!;

    public string Season { get; set; } = null!;

    public string? Category { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public string? CampaignName { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;
}

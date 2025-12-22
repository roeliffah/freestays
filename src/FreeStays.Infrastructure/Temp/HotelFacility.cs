using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class HotelFacility
{
    public Guid Id { get; set; }

    public Guid HotelId { get; set; }

    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class HotelImage
{
    public Guid Id { get; set; }

    public Guid HotelId { get; set; }

    public string Url { get; set; } = null!;

    public int Order { get; set; }

    public string? Caption { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class SiteSetting
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string Group { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Translation
{
    public Guid Id { get; set; }

    public string Locale { get; set; } = null!;

    public string Namespace { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public Guid? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}

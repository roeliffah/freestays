using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class SeoSetting
{
    public Guid Id { get; set; }

    public string Locale { get; set; } = null!;

    public string PageType { get; set; } = null!;

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public string? MetaKeywords { get; set; }

    public string? OgImage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

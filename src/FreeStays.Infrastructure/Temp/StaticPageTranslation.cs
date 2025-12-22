using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class StaticPageTranslation
{
    public Guid Id { get; set; }

    public Guid PageId { get; set; }

    public string Locale { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual StaticPage Page { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class StaticPage
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<StaticPageTranslation> StaticPageTranslations { get; set; } = new List<StaticPageTranslation>();
}

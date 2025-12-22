using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Faq
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }

    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<FaqTranslation> FaqTranslations { get; set; } = new List<FaqTranslation>();
}

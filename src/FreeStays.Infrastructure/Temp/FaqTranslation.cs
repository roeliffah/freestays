using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class FaqTranslation
{
    public Guid Id { get; set; }

    public Guid FaqId { get; set; }

    public string Locale { get; set; } = null!;

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Faq Faq { get; set; } = null!;
}

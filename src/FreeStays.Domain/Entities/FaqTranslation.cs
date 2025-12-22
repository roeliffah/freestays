using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class FaqTranslation : BaseEntity
{
    public Guid FaqId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    
    // Navigation
    public Faq Faq { get; set; } = null!;
}

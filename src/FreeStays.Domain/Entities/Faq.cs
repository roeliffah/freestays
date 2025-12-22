using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class Faq : BaseEntity
{
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Category { get; set; }
    
    // Navigation
    public ICollection<FaqTranslation> Translations { get; set; } = new List<FaqTranslation>();
}

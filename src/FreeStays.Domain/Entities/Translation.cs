using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class Translation : BaseEntity
{
    public string Locale { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Guid? UpdatedBy { get; set; }
    
    // Navigation
    public User? UpdatedByUser { get; set; }
}

using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class SiteSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = "{}"; // JSON
    public string Group { get; set; } = "general"; // general, seo, payment, email, social
}

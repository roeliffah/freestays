namespace FreeStays.Application.DTOs.Faqs;

public class FaqDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<FaqTranslationDto> Translations { get; set; } = new();
}

public class FaqTranslationDto
{
    public Guid Id { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class CreateFaqTranslationDto
{
    public string Locale { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

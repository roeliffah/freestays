namespace FreeStays.Application.DTOs.Translations;

public record TranslationDto
{
    public Guid Id { get; init; }
    public string Locale { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? UpdatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record TranslationsByNamespaceDto
{
    public string Locale { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public Dictionary<string, string> Translations { get; init; } = new();
}

public record UpdateTranslationDto
{
    public string Value { get; init; } = string.Empty;
}

public record CreateTranslationDto
{
    public string Locale { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public record BulkUpdateTranslationDto
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

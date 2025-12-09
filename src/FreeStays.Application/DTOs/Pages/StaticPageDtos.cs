namespace FreeStays.Application.DTOs.Pages;

public record StaticPageDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<StaticPageTranslationDto> Translations { get; init; } = new();
}

public record StaticPageTranslationDto
{
    public Guid Id { get; init; }
    public string Locale { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public record StaticPageContentDto
{
    public string Slug { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public record CreateStaticPageDto
{
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public List<CreateStaticPageTranslationDto> Translations { get; init; } = new();
}

public record CreateStaticPageTranslationDto
{
    public string Locale { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public record UpdateStaticPageDto
{
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<CreateStaticPageTranslationDto> Translations { get; init; } = new();
}

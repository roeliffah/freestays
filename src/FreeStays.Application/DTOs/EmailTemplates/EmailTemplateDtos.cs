namespace FreeStays.Application.DTOs.EmailTemplates;

public record EmailTemplateDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? Variables { get; init; }
    public bool IsActive { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record UpdateEmailTemplateDto
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? Variables { get; init; }
    public bool IsActive { get; init; }
}

public record TestEmailDto
{
    public string To { get; init; } = string.Empty;
    public Dictionary<string, string>? Variables { get; init; }
}

namespace FreeStays.Application.DTOs.EmailTemplates;

public record EmailTemplateDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public Dictionary<string, string> Subject { get; init; } = new();
    public Dictionary<string, string> Body { get; init; } = new();
    public List<string> Variables { get; init; } = new();
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record EmailTemplateLocalizedDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public List<string> Variables { get; init; } = new();
    public bool IsActive { get; init; }
    public string Locale { get; init; } = string.Empty;
}

public record CreateEmailTemplateRequest
{
    public string Code { get; init; } = string.Empty;
    public Dictionary<string, string> Subject { get; init; } = new();
    public Dictionary<string, string> Body { get; init; } = new();
    public List<string> Variables { get; init; } = new();
    public bool IsActive { get; init; } = true;
}

public record UpdateEmailTemplateRequest
{
    public Dictionary<string, string>? Subject { get; init; }
    public Dictionary<string, string>? Body { get; init; }
    public List<string>? Variables { get; init; }
    public bool? IsActive { get; init; }
}

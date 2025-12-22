using System.Text.Json;
using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Queries;

public record GetAllEmailTemplatesQuery : IRequest<List<EmailTemplateDto>>
{
    public bool? IsActive { get; init; }
}

public class GetAllEmailTemplatesQueryHandler : IRequestHandler<GetAllEmailTemplatesQuery, List<EmailTemplateDto>>
{
    private readonly IEmailTemplateRepository _repository;

    public GetAllEmailTemplatesQueryHandler(IEmailTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<EmailTemplateDto>> Handle(GetAllEmailTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.GetAllAsync(cancellationToken);

        if (request.IsActive.HasValue)
        {
            templates = templates.Where(t => t.IsActive == request.IsActive.Value).ToList();
        }

        return templates.Select(t => new EmailTemplateDto
        {
            Id = t.Id,
            Code = t.Code,
            Subject = JsonSerializer.Deserialize<Dictionary<string, string>>(t.Subject) ?? new(),
            Body = JsonSerializer.Deserialize<Dictionary<string, string>>(t.Body) ?? new(),
            Variables = JsonSerializer.Deserialize<List<string>>(t.Variables) ?? new(),
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt
        }).ToList();
    }
}

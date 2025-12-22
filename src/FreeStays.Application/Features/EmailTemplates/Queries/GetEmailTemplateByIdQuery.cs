using System.Text.Json;
using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Queries;

public record GetEmailTemplateByIdQuery(Guid Id) : IRequest<EmailTemplateDto>;

public class GetEmailTemplateByIdQueryHandler : IRequestHandler<GetEmailTemplateByIdQuery, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _repository;

    public GetEmailTemplateByIdQueryHandler(IEmailTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<EmailTemplateDto> Handle(GetEmailTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException("EmailTemplate", request.Id.ToString());
        }

        return new EmailTemplateDto
        {
            Id = template.Id,
            Code = template.Code,
            Subject = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Subject) ?? new(),
            Body = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Body) ?? new(),
            Variables = JsonSerializer.Deserialize<List<string>>(template.Variables) ?? new(),
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt
        };
    }
}

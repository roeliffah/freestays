using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Queries;

public record GetEmailTemplatesQuery : IRequest<List<EmailTemplateDto>>;

public class GetEmailTemplatesQueryHandler : IRequestHandler<GetEmailTemplatesQuery, List<EmailTemplateDto>>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;

    public GetEmailTemplatesQueryHandler(IEmailTemplateRepository emailTemplateRepository)
    {
        _emailTemplateRepository = emailTemplateRepository;
    }

    public async Task<List<EmailTemplateDto>> Handle(GetEmailTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _emailTemplateRepository.GetAllAsync(cancellationToken);

        return templates.Select(t => new EmailTemplateDto
        {
            Id = t.Id,
            Code = t.Code,
            Subject = t.Subject,
            Body = t.Body,
            Variables = t.Variables,
            IsActive = t.IsActive,
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }
}

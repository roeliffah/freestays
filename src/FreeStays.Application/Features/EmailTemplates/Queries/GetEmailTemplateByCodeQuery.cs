using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Queries;

public record GetEmailTemplateByCodeQuery(string Code) : IRequest<EmailTemplateDto>;

public class GetEmailTemplateByCodeQueryHandler : IRequestHandler<GetEmailTemplateByCodeQuery, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;

    public GetEmailTemplateByCodeQueryHandler(IEmailTemplateRepository emailTemplateRepository)
    {
        _emailTemplateRepository = emailTemplateRepository;
    }

    public async Task<EmailTemplateDto> Handle(GetEmailTemplateByCodeQuery request, CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException("EmailTemplate", request.Code);
        }

        return new EmailTemplateDto
        {
            Id = template.Id,
            Code = template.Code,
            Subject = template.Subject,
            Body = template.Body,
            Variables = template.Variables,
            IsActive = template.IsActive,
            UpdatedAt = template.UpdatedAt
        };
    }
}

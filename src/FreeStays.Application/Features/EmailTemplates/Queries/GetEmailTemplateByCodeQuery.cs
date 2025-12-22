using System.Text.Json;
using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Queries;

public record GetEmailTemplateByCodeQuery(string Code, string Locale = "tr") : IRequest<EmailTemplateLocalizedDto>;

public class GetEmailTemplateByCodeQueryHandler : IRequestHandler<GetEmailTemplateByCodeQuery, EmailTemplateLocalizedDto>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;

    public GetEmailTemplateByCodeQueryHandler(IEmailTemplateRepository emailTemplateRepository)
    {
        _emailTemplateRepository = emailTemplateRepository;
    }

    public async Task<EmailTemplateLocalizedDto> Handle(GetEmailTemplateByCodeQuery request, CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException("EmailTemplate", request.Code);
        }

        if (!template.IsActive)
        {
            throw new ValidationException("EmailTemplate", "Bu e-posta şablonu pasif durumda.");
        }

        var subjects = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Subject) ?? new();
        var bodies = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Body) ?? new();
        var variables = JsonSerializer.Deserialize<List<string>>(template.Variables) ?? new();

        // İlgili dile göre subject ve body al, yoksa fallback olarak tr veya ilk mevcut dil
        var subject = subjects.GetValueOrDefault(request.Locale)
            ?? subjects.GetValueOrDefault("tr")
            ?? subjects.Values.FirstOrDefault()
            ?? string.Empty;

        var body = bodies.GetValueOrDefault(request.Locale)
            ?? bodies.GetValueOrDefault("tr")
            ?? bodies.Values.FirstOrDefault()
            ?? string.Empty;

        return new EmailTemplateLocalizedDto
        {
            Id = template.Id,
            Code = template.Code,
            Subject = subject,
            Body = body,
            Variables = variables,
            IsActive = template.IsActive,
            Locale = request.Locale
        };
    }
}

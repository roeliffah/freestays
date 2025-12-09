using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FreeStays.Application.Features.EmailTemplates.Commands;

public record SendTestEmailCommand : IRequest<bool>
{
    public string Code { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public Dictionary<string, string>? Variables { get; init; }
}

public class SendTestEmailCommandHandler : IRequestHandler<SendTestEmailCommand, bool>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ILogger<SendTestEmailCommandHandler> _logger;

    public SendTestEmailCommandHandler(
        IEmailTemplateRepository emailTemplateRepository,
        ILogger<SendTestEmailCommandHandler> logger)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException("EmailTemplate", request.Code);
        }

        // Replace variables in subject and body
        var subject = template.Subject;
        var body = template.Body;

        if (request.Variables != null)
        {
            foreach (var variable in request.Variables)
            {
                subject = subject.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                body = body.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }
        }

        // TODO: Implement actual email sending via IEmailService
        _logger.LogInformation("Test email sent to {To} with template {Code}. Subject: {Subject}", 
            request.To, request.Code, subject);

        return true;
    }
}

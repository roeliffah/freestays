using System.Text.Json;
using FluentValidation;
using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Commands;

public record UpdateEmailTemplateCommand : IRequest<EmailTemplateDto>
{
    public Guid Id { get; init; }
    public Dictionary<string, string>? Subject { get; init; }
    public Dictionary<string, string>? Body { get; init; }
    public List<string>? Variables { get; init; }
    public bool? IsActive { get; init; }
}

public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateCommandValidator()
    {
        When(x => x.Subject != null, () =>
        {
            RuleFor(x => x.Subject!)
                .Must(s => s.ContainsKey("tr") && s.ContainsKey("en"))
                .WithMessage("TR ve EN dilleri için konu başlığı gereklidir.");
        });

        When(x => x.Body != null, () =>
        {
            RuleFor(x => x.Body!)
                .Must(b => b.ContainsKey("tr") && b.ContainsKey("en"))
                .WithMessage("TR ve EN dilleri için e-posta içeriği gereklidir.");
        });
    }
}

public class UpdateEmailTemplateCommandHandler : IRequestHandler<UpdateEmailTemplateCommand, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailTemplateCommandHandler(IEmailTemplateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailTemplateDto> Handle(UpdateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException("EmailTemplate", request.Id.ToString());
        }

        var currentSubject = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Subject) ?? new();
        var currentBody = JsonSerializer.Deserialize<Dictionary<string, string>>(template.Body) ?? new();
        var currentVariables = JsonSerializer.Deserialize<List<string>>(template.Variables) ?? new();

        if (request.Subject != null)
        {
            template.Subject = JsonSerializer.Serialize(request.Subject);
            currentSubject = request.Subject;
        }

        if (request.Body != null)
        {
            template.Body = JsonSerializer.Serialize(request.Body);
            currentBody = request.Body;
        }

        if (request.Variables != null)
        {
            template.Variables = JsonSerializer.Serialize(request.Variables);
            currentVariables = request.Variables;
        }

        if (request.IsActive.HasValue)
        {
            template.IsActive = request.IsActive.Value;
        }

        await _repository.UpdateAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EmailTemplateDto
        {
            Id = template.Id,
            Code = template.Code,
            Subject = currentSubject,
            Body = currentBody,
            Variables = currentVariables,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt
        };
    }
}

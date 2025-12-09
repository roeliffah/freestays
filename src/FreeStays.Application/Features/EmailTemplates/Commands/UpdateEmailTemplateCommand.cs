using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Commands;

public record UpdateEmailTemplateCommand : IRequest<EmailTemplateDto>
{
    public string Code { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? Variables { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class UpdateEmailTemplateCommandHandler : IRequestHandler<UpdateEmailTemplateCommand, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailTemplateCommandHandler(IEmailTemplateRepository emailTemplateRepository, IUnitOfWork unitOfWork)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailTemplateDto> Handle(UpdateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (template == null)
        {
            template = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Subject = request.Subject,
                Body = request.Body,
                Variables = request.Variables,
                IsActive = request.IsActive
            };
            await _emailTemplateRepository.AddAsync(template, cancellationToken);
        }
        else
        {
            template.Subject = request.Subject;
            template.Body = request.Body;
            template.Variables = request.Variables;
            template.IsActive = request.IsActive;
            template.UpdatedAt = DateTime.UtcNow;
            await _emailTemplateRepository.UpdateAsync(template, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

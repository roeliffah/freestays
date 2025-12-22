using System.Text.Json;
using FluentValidation;
using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;
using DomainValidationException = FreeStays.Domain.Exceptions.ValidationException;

namespace FreeStays.Application.Features.EmailTemplates.Commands;

public record CreateEmailTemplateCommand : IRequest<EmailTemplateDto>
{
    public string Code { get; init; } = string.Empty;
    public Dictionary<string, string> Subject { get; init; } = new();
    public Dictionary<string, string> Body { get; init; } = new();
    public List<string> Variables { get; init; } = new();
    public bool IsActive { get; init; } = true;
}

public class CreateEmailTemplateCommandValidator : AbstractValidator<CreateEmailTemplateCommand>
{
    public CreateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Şablon kodu gereklidir.")
            .MaximumLength(100).WithMessage("Şablon kodu 100 karakterden uzun olamaz.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Konu başlığı gereklidir.")
            .Must(s => s.ContainsKey("tr") && s.ContainsKey("en"))
            .WithMessage("TR ve EN dilleri için konu başlığı gereklidir.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("E-posta içeriği gereklidir.")
            .Must(b => b.ContainsKey("tr") && b.ContainsKey("en"))
            .WithMessage("TR ve EN dilleri için e-posta içeriği gereklidir.");
    }
}

public class CreateEmailTemplateCommandHandler : IRequestHandler<CreateEmailTemplateCommand, EmailTemplateDto>
{
    private readonly IEmailTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmailTemplateCommandHandler(IEmailTemplateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailTemplateDto> Handle(CreateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        // Aynı code ile şablon var mı kontrol et
        var existing = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing != null)
        {
            throw new DomainValidationException("EmailTemplate", $"'{request.Code}' kodlu şablon zaten mevcut.");
        }

        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Subject = JsonSerializer.Serialize(request.Subject),
            Body = JsonSerializer.Serialize(request.Body),
            Variables = JsonSerializer.Serialize(request.Variables),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EmailTemplateDto
        {
            Id = template.Id,
            Code = template.Code,
            Subject = request.Subject,
            Body = request.Body,
            Variables = request.Variables,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt
        };
    }
}

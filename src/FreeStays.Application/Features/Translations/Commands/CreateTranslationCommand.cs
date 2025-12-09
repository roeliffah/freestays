using FreeStays.Application.DTOs.Translations;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace FreeStays.Application.Features.Translations.Commands;

public record CreateTranslationCommand : IRequest<TranslationDto>
{
    public string Locale { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? UpdatedBy { get; init; }
}

public class CreateTranslationCommandValidator : AbstractValidator<CreateTranslationCommand>
{
    public CreateTranslationCommandValidator()
    {
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Namespace).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).NotEmpty();
    }
}

public class CreateTranslationCommandHandler : IRequestHandler<CreateTranslationCommand, TranslationDto>
{
    private readonly ITranslationRepository _translationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTranslationCommandHandler(ITranslationRepository translationRepository, IUnitOfWork unitOfWork)
    {
        _translationRepository = translationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TranslationDto> Handle(CreateTranslationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _translationRepository.GetByKeyAsync(
            request.Locale, 
            request.Namespace, 
            request.Key, 
            cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"Translation with key '{request.Key}' already exists in namespace '{request.Namespace}' for locale '{request.Locale}'.");
        }

        var translation = new Translation
        {
            Id = Guid.NewGuid(),
            Locale = request.Locale,
            Namespace = request.Namespace,
            Key = request.Key,
            Value = request.Value,
            UpdatedBy = Guid.TryParse(request.UpdatedBy, out var userId) ? userId : null
        };

        await _translationRepository.AddAsync(translation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TranslationDto
        {
            Id = translation.Id,
            Locale = translation.Locale,
            Namespace = translation.Namespace,
            Key = translation.Key,
            Value = translation.Value,
            UpdatedBy = translation.UpdatedBy?.ToString(),
            UpdatedAt = translation.UpdatedAt
        };
    }
}

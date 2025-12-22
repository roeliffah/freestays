using FluentValidation;
using FreeStays.Application.DTOs.Faqs;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Faqs.Commands;

public record UpdateFaqCommand : IRequest<FaqDto>
{
    public Guid Id { get; init; }
    public int Order { get; init; }
    public bool IsActive { get; init; }
    public string? Category { get; init; }
    public List<CreateFaqTranslationDto> Translations { get; init; } = new();
}

public class UpdateFaqCommandValidator : AbstractValidator<UpdateFaqCommand>
{
    public UpdateFaqCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        
        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Translations).ChildRules(translation =>
        {
            translation.RuleFor(t => t.Locale)
                .NotEmpty().WithMessage("Locale is required.")
                .MaximumLength(10);
            
            translation.RuleFor(t => t.Question)
                .NotEmpty().WithMessage("Question is required.")
                .MaximumLength(500);
            
            translation.RuleFor(t => t.Answer)
                .NotEmpty().WithMessage("Answer is required.");
        });
    }
}

public class UpdateFaqCommandHandler : IRequestHandler<UpdateFaqCommand, FaqDto>
{
    private readonly IFaqRepository _faqRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFaqCommandHandler(IFaqRepository faqRepository, IUnitOfWork unitOfWork)
    {
        _faqRepository = faqRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FaqDto> Handle(UpdateFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = await _faqRepository.GetByIdAsync(request.Id, cancellationToken);

        if (faq == null)
        {
            throw new NotFoundException("Faq", request.Id);
        }

        faq.Order = request.Order;
        faq.IsActive = request.IsActive;
        faq.Category = request.Category;

        // Delete existing translations
        await _faqRepository.DeleteTranslationsAsync(request.Id, cancellationToken);

        // Add new translations
        foreach (var translation in request.Translations)
        {
            await _faqRepository.AddTranslationAsync(new FaqTranslation
            {
                Id = Guid.NewGuid(),
                FaqId = faq.Id,
                Locale = translation.Locale,
                Question = translation.Question,
                Answer = translation.Answer
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with translations
        var updatedFaq = await _faqRepository.GetByIdWithTranslationsAsync(request.Id, cancellationToken);

        return new FaqDto
        {
            Id = updatedFaq!.Id,
            Order = updatedFaq.Order,
            IsActive = updatedFaq.IsActive,
            Category = updatedFaq.Category,
            CreatedAt = updatedFaq.CreatedAt,
            UpdatedAt = updatedFaq.UpdatedAt,
            Translations = updatedFaq.Translations.Select(t => new FaqTranslationDto
            {
                Id = t.Id,
                Locale = t.Locale,
                Question = t.Question,
                Answer = t.Answer
            }).ToList()
        };
    }
}

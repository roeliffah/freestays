using FluentValidation;
using FreeStays.Application.DTOs.Faqs;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Faqs.Commands;

public record CreateFaqCommand : IRequest<FaqDto>
{
    public int Order { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Category { get; init; }
    public List<CreateFaqTranslationDto> Translations { get; init; } = new();
}

public class CreateFaqCommandValidator : AbstractValidator<CreateFaqCommand>
{
    public CreateFaqCommandValidator()
    {
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

public class CreateFaqCommandHandler : IRequestHandler<CreateFaqCommand, FaqDto>
{
    private readonly IFaqRepository _faqRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFaqCommandHandler(IFaqRepository faqRepository, IUnitOfWork unitOfWork)
    {
        _faqRepository = faqRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FaqDto> Handle(CreateFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = new Faq
        {
            Id = Guid.NewGuid(),
            Order = request.Order,
            IsActive = request.IsActive,
            Category = request.Category
        };

        foreach (var translation in request.Translations)
        {
            faq.Translations.Add(new FaqTranslation
            {
                Id = Guid.NewGuid(),
                FaqId = faq.Id,
                Locale = translation.Locale,
                Question = translation.Question,
                Answer = translation.Answer
            });
        }

        await _faqRepository.AddAsync(faq, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FaqDto
        {
            Id = faq.Id,
            Order = faq.Order,
            IsActive = faq.IsActive,
            Category = faq.Category,
            CreatedAt = faq.CreatedAt,
            UpdatedAt = faq.UpdatedAt,
            Translations = faq.Translations.Select(t => new FaqTranslationDto
            {
                Id = t.Id,
                Locale = t.Locale,
                Question = t.Question,
                Answer = t.Answer
            }).ToList()
        };
    }
}

using FluentValidation;
using FreeStays.Application.DTOs.Faqs;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Faqs.Queries;

public record GetFaqByIdQuery(Guid Id) : IRequest<FaqDto>;

public class GetFaqByIdQueryValidator : AbstractValidator<GetFaqByIdQuery>
{
    public GetFaqByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetFaqByIdQueryHandler : IRequestHandler<GetFaqByIdQuery, FaqDto>
{
    private readonly IFaqRepository _faqRepository;

    public GetFaqByIdQueryHandler(IFaqRepository faqRepository)
    {
        _faqRepository = faqRepository;
    }

    public async Task<FaqDto> Handle(GetFaqByIdQuery request, CancellationToken cancellationToken)
    {
        var faq = await _faqRepository.GetByIdWithTranslationsAsync(request.Id, cancellationToken);

        if (faq == null)
        {
            throw new NotFoundException("Faq", request.Id);
        }

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

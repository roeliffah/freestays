using FreeStays.Application.DTOs.Faqs;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Faqs.Queries;

public record GetActiveFaqsQuery(string? Locale = null, string? Category = null) : IRequest<List<FaqDto>>;

public class GetActiveFaqsQueryHandler : IRequestHandler<GetActiveFaqsQuery, List<FaqDto>>
{
    private readonly IFaqRepository _faqRepository;

    public GetActiveFaqsQueryHandler(IFaqRepository faqRepository)
    {
        _faqRepository = faqRepository;
    }

    public async Task<List<FaqDto>> Handle(GetActiveFaqsQuery request, CancellationToken cancellationToken)
    {
        var faqs = string.IsNullOrEmpty(request.Category)
            ? await _faqRepository.GetActiveWithTranslationsAsync(request.Locale, cancellationToken)
            : await _faqRepository.GetByCategoryAsync(request.Category, request.Locale, cancellationToken);

        return faqs.Select(f => new FaqDto
        {
            Id = f.Id,
            Order = f.Order,
            IsActive = f.IsActive,
            Category = f.Category,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
            Translations = f.Translations
                .Where(t => string.IsNullOrEmpty(request.Locale) || t.Locale == request.Locale)
                .Select(t => new FaqTranslationDto
                {
                    Id = t.Id,
                    Locale = t.Locale,
                    Question = t.Question,
                    Answer = t.Answer
                }).ToList()
        }).ToList();
    }
}

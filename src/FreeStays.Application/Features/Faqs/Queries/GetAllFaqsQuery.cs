using FreeStays.Application.DTOs.Faqs;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Faqs.Queries;

public record GetAllFaqsQuery : IRequest<List<FaqDto>>;

public class GetAllFaqsQueryHandler : IRequestHandler<GetAllFaqsQuery, List<FaqDto>>
{
    private readonly IFaqRepository _faqRepository;

    public GetAllFaqsQueryHandler(IFaqRepository faqRepository)
    {
        _faqRepository = faqRepository;
    }

    public async Task<List<FaqDto>> Handle(GetAllFaqsQuery request, CancellationToken cancellationToken)
    {
        var faqs = await _faqRepository.GetAllWithTranslationsAsync(cancellationToken);

        return faqs.Select(f => new FaqDto
        {
            Id = f.Id,
            Order = f.Order,
            IsActive = f.IsActive,
            Category = f.Category,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt,
            Translations = f.Translations.Select(t => new FaqTranslationDto
            {
                Id = t.Id,
                Locale = t.Locale,
                Question = t.Question,
                Answer = t.Answer
            }).ToList()
        }).ToList();
    }
}

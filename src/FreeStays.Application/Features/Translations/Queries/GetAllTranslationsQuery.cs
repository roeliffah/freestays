using FreeStays.Application.DTOs.Translations;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Translations.Queries;

public record GetAllTranslationsQuery(string Locale) : IRequest<List<TranslationDto>>;

public class GetAllTranslationsQueryHandler : IRequestHandler<GetAllTranslationsQuery, List<TranslationDto>>
{
    private readonly ITranslationRepository _translationRepository;

    public GetAllTranslationsQueryHandler(ITranslationRepository translationRepository)
    {
        _translationRepository = translationRepository;
    }

    public async Task<List<TranslationDto>> Handle(GetAllTranslationsQuery request, CancellationToken cancellationToken)
    {
        var translations = await _translationRepository.GetByLocaleAsync(request.Locale, cancellationToken);
        
        return translations.Select(t => new TranslationDto
        {
            Id = t.Id,
            Locale = t.Locale,
            Namespace = t.Namespace,
            Key = t.Key,
            Value = t.Value,
            UpdatedBy = t.UpdatedBy?.ToString(),
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }
}

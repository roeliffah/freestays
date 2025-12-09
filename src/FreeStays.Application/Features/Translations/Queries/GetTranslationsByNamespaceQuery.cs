using FreeStays.Application.DTOs.Translations;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Translations.Queries;

public record GetTranslationsByNamespaceQuery(string Locale, string Namespace) : IRequest<TranslationsByNamespaceDto>;

public class GetTranslationsByNamespaceQueryHandler : IRequestHandler<GetTranslationsByNamespaceQuery, TranslationsByNamespaceDto>
{
    private readonly ITranslationRepository _translationRepository;

    public GetTranslationsByNamespaceQueryHandler(ITranslationRepository translationRepository)
    {
        _translationRepository = translationRepository;
    }

    public async Task<TranslationsByNamespaceDto> Handle(GetTranslationsByNamespaceQuery request, CancellationToken cancellationToken)
    {
        var translations = await _translationRepository.GetByNamespaceAsync(request.Locale, request.Namespace, cancellationToken);
        
        return new TranslationsByNamespaceDto
        {
            Locale = request.Locale,
            Namespace = request.Namespace,
            Translations = translations.ToDictionary(t => t.Key, t => t.Value)
        };
    }
}

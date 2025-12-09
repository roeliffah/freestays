using FreeStays.Application.DTOs.Pages;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Pages.Queries;

public record GetStaticPageBySlugQuery(string Slug, string Locale) : IRequest<StaticPageContentDto?>;

public class GetStaticPageBySlugQueryHandler : IRequestHandler<GetStaticPageBySlugQuery, StaticPageContentDto?>
{
    private readonly IStaticPageRepository _pageRepository;

    public GetStaticPageBySlugQueryHandler(IStaticPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<StaticPageContentDto?> Handle(GetStaticPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var page = await _pageRepository.GetBySlugWithTranslationsAsync(request.Slug, cancellationToken);
        
        if (page == null || !page.IsActive)
            return null;

        var translation = page.Translations.FirstOrDefault(t => t.Locale == request.Locale)
                         ?? page.Translations.FirstOrDefault(t => t.Locale == "en")
                         ?? page.Translations.FirstOrDefault();

        if (translation == null)
            return null;

        return new StaticPageContentDto
        {
            Slug = page.Slug,
            Locale = translation.Locale,
            Title = translation.Title,
            Content = translation.Content,
            MetaTitle = translation.MetaTitle,
            MetaDescription = translation.MetaDescription
        };
    }
}

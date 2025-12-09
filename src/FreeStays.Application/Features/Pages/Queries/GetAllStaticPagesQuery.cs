using FreeStays.Application.DTOs.Pages;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Pages.Queries;

public record GetAllStaticPagesQuery : IRequest<List<StaticPageDto>>;

public class GetAllStaticPagesQueryHandler : IRequestHandler<GetAllStaticPagesQuery, List<StaticPageDto>>
{
    private readonly IStaticPageRepository _pageRepository;

    public GetAllStaticPagesQueryHandler(IStaticPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<List<StaticPageDto>> Handle(GetAllStaticPagesQuery request, CancellationToken cancellationToken)
    {
        var pages = await _pageRepository.GetAllWithTranslationsAsync(cancellationToken);
        
        return pages.Select(p => new StaticPageDto
        {
            Id = p.Id,
            Slug = p.Slug,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Translations = p.Translations.Select(t => new StaticPageTranslationDto
            {
                Id = t.Id,
                Locale = t.Locale,
                Title = t.Title,
                Content = t.Content,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription
            }).ToList()
        }).ToList();
    }
}

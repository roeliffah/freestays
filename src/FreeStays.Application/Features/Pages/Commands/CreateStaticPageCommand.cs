using FreeStays.Application.DTOs.Pages;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace FreeStays.Application.Features.Pages.Commands;

public record CreateStaticPageCommand : IRequest<StaticPageDto>
{
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public List<CreateStaticPageTranslationDto> Translations { get; init; } = new();
}

public class CreateStaticPageCommandValidator : AbstractValidator<CreateStaticPageCommand>
{
    public CreateStaticPageCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase and can only contain letters, numbers, and hyphens.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Translations).ChildRules(translation =>
        {
            translation.RuleFor(t => t.Locale).NotEmpty().WithMessage("Locale is required.");
            translation.RuleFor(t => t.Title).NotEmpty().WithMessage("Title is required.");
            translation.RuleFor(t => t.Content).NotEmpty().WithMessage("Content is required.");
        });
    }
}

public class CreateStaticPageCommandHandler : IRequestHandler<CreateStaticPageCommand, StaticPageDto>
{
    private readonly IStaticPageRepository _pageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStaticPageCommandHandler(IStaticPageRepository pageRepository, IUnitOfWork unitOfWork)
    {
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StaticPageDto> Handle(CreateStaticPageCommand request, CancellationToken cancellationToken)
    {
        if (await _pageRepository.SlugExistsAsync(request.Slug, null, cancellationToken))
        {
            throw new InvalidOperationException($"A page with slug '{request.Slug}' already exists.");
        }

        var page = new StaticPage
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            IsActive = request.IsActive,
            Translations = request.Translations.Select(t => new StaticPageTranslation
            {
                Id = Guid.NewGuid(),
                Locale = t.Locale,
                Title = t.Title,
                Content = t.Content,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription
            }).ToList()
        };

        await _pageRepository.AddAsync(page, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StaticPageDto
        {
            Id = page.Id,
            Slug = page.Slug,
            IsActive = page.IsActive,
            CreatedAt = page.CreatedAt,
            UpdatedAt = page.UpdatedAt,
            Translations = page.Translations.Select(t => new StaticPageTranslationDto
            {
                Id = t.Id,
                Locale = t.Locale,
                Title = t.Title,
                Content = t.Content,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription
            }).ToList()
        };
    }
}

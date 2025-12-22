using FluentValidation;
using FreeStays.Application.DTOs.Pages;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Pages.Commands;

public record UpdateStaticPageCommand : IRequest<StaticPageDto>
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<CreateStaticPageTranslationDto> Translations { get; init; } = new();
}

public class UpdateStaticPageCommandValidator : AbstractValidator<UpdateStaticPageCommand>
{
    public UpdateStaticPageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase and can only contain letters, numbers, and hyphens.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");
    }
}

public class UpdateStaticPageCommandHandler : IRequestHandler<UpdateStaticPageCommand, StaticPageDto>
{
    private readonly IStaticPageRepository _pageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStaticPageCommandHandler(
        IStaticPageRepository pageRepository, 
        IUnitOfWork unitOfWork)
    {
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StaticPageDto> Handle(UpdateStaticPageCommand request, CancellationToken cancellationToken)
    {
        // Check if page exists
        var page = await _pageRepository.GetByIdAsync(request.Id, cancellationToken);

        if (page == null)
        {
            throw new NotFoundException("StaticPage", request.Id);
        }

        if (await _pageRepository.SlugExistsAsync(request.Slug, request.Id, cancellationToken))
        {
            throw new InvalidOperationException($"A page with slug '{request.Slug}' already exists.");
        }

        // Update page properties - DON'T set UpdatedAt manually, DbContext will handle it
        page.Slug = request.Slug;
        page.IsActive = request.IsActive;

        // Delete all existing translations
        await _pageRepository.DeleteTranslationsAsync(request.Id, cancellationToken);

        // Add new translations via repository (not through page.Translations)
        foreach (var translation in request.Translations)
        {
            await _pageRepository.AddTranslationAsync(new StaticPageTranslation
            {
                Id = Guid.NewGuid(),
                PageId = page.Id,
                Locale = translation.Locale,
                Title = translation.Title,
                Content = translation.Content,
                MetaTitle = translation.MetaTitle,
                MetaDescription = translation.MetaDescription
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload the page with new translations
        var updatedPage = await _pageRepository.GetByIdWithTranslationsAsync(request.Id, cancellationToken);

        return new StaticPageDto
        {
            Id = updatedPage!.Id,
            Slug = updatedPage.Slug,
            IsActive = updatedPage.IsActive,
            CreatedAt = updatedPage.CreatedAt,
            UpdatedAt = updatedPage.UpdatedAt,
            Translations = updatedPage.Translations.Select(t => new StaticPageTranslationDto
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

using FreeStays.Application.DTOs.Pages;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using FluentValidation;
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

    public UpdateStaticPageCommandHandler(IStaticPageRepository pageRepository, IUnitOfWork unitOfWork)
    {
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StaticPageDto> Handle(UpdateStaticPageCommand request, CancellationToken cancellationToken)
    {
        var page = await _pageRepository.GetBySlugWithTranslationsAsync(request.Slug, cancellationToken);
        
        if (page == null || page.Id != request.Id)
        {
            page = await _pageRepository.GetByIdAsync(request.Id, cancellationToken);
        }

        if (page == null)
        {
            throw new NotFoundException("StaticPage", request.Id);
        }

        if (await _pageRepository.SlugExistsAsync(request.Slug, request.Id, cancellationToken))
        {
            throw new InvalidOperationException($"A page with slug '{request.Slug}' already exists.");
        }

        page.Slug = request.Slug;
        page.IsActive = request.IsActive;
        page.UpdatedAt = DateTime.UtcNow;

        // Clear existing translations and add new ones
        page.Translations.Clear();
        foreach (var translation in request.Translations)
        {
            page.Translations.Add(new StaticPageTranslation
            {
                Id = Guid.NewGuid(),
                PageId = page.Id,
                Locale = translation.Locale,
                Title = translation.Title,
                Content = translation.Content,
                MetaTitle = translation.MetaTitle,
                MetaDescription = translation.MetaDescription
            });
        }

        await _pageRepository.UpdateAsync(page, cancellationToken);
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

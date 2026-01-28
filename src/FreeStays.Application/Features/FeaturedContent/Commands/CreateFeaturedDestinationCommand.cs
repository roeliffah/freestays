using FluentValidation;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Commands;

public record CreateFeaturedDestinationCommand : IRequest<FeaturedDestinationDto>
{
    public CreateFeaturedDestinationDto Data { get; init; } = new();
}

public class CreateFeaturedDestinationCommandValidator : AbstractValidator<CreateFeaturedDestinationCommand>
{
    public CreateFeaturedDestinationCommandValidator()
    {
        RuleFor(x => x.Data.DestinationId).NotEmpty();
        RuleFor(x => x.Data.DestinationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.CountryCode).NotEmpty().Length(2);
        RuleFor(x => x.Data.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.Priority).GreaterThanOrEqualTo(0);
    }
}

public class CreateFeaturedDestinationCommandHandler : IRequestHandler<CreateFeaturedDestinationCommand, FeaturedDestinationDto>
{
    private readonly IFeaturedDestinationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPopularDestinationWarmupService _warmupService;

    public CreateFeaturedDestinationCommandHandler(IFeaturedDestinationRepository repository, IUnitOfWork unitOfWork, IPopularDestinationWarmupService warmupService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _warmupService = warmupService;
    }

    public async Task<FeaturedDestinationDto> Handle(CreateFeaturedDestinationCommand request, CancellationToken cancellationToken)
    {
        var featuredDestination = new FeaturedDestination
        {
            Id = Guid.NewGuid(),
            DestinationId = request.Data.DestinationId,
            DestinationName = request.Data.DestinationName,
            CountryCode = request.Data.CountryCode,
            Country = request.Data.Country,
            Priority = request.Data.Priority,
            Status = request.Data.Status,
            Season = request.Data.Season,
            Image = request.Data.Image,
            Description = request.Data.Description,
            ValidFrom = request.Data.ValidFrom,
            ValidUntil = request.Data.ValidUntil
        };

        await _repository.AddAsync(featuredDestination, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ✅ Admin kaydı sonrası: popüler destinasyon için DB cache warmup'ı başlat
        await _warmupService.WarmDestinationAsync(featuredDestination.DestinationId, cancellationToken);

        return new FeaturedDestinationDto
        {
            Id = featuredDestination.Id,
            DestinationId = featuredDestination.DestinationId,
            DestinationName = featuredDestination.DestinationName,
            CountryCode = featuredDestination.CountryCode,
            Country = featuredDestination.Country,
            Priority = featuredDestination.Priority,
            Status = featuredDestination.Status.ToString(),
            Season = featuredDestination.Season.ToString(),
            Image = featuredDestination.Image,
            Description = featuredDestination.Description,
            ValidFrom = featuredDestination.ValidFrom,
            ValidUntil = featuredDestination.ValidUntil,
            CreatedAt = featuredDestination.CreatedAt,
            UpdatedAt = featuredDestination.UpdatedAt
        };
    }
}

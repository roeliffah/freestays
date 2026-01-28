using FluentValidation;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Commands;

public record UpdateFeaturedDestinationCommand : IRequest<FeaturedDestinationDto>
{
    public Guid Id { get; init; }
    public UpdateFeaturedDestinationDto Data { get; init; } = new();
}

public class UpdateFeaturedDestinationCommandValidator : AbstractValidator<UpdateFeaturedDestinationCommand>
{
    public UpdateFeaturedDestinationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateFeaturedDestinationCommandHandler : IRequestHandler<UpdateFeaturedDestinationCommand, FeaturedDestinationDto>
{
    private readonly IFeaturedDestinationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPopularDestinationWarmupService _warmupService;

    public UpdateFeaturedDestinationCommandHandler(IFeaturedDestinationRepository repository, IUnitOfWork unitOfWork, IPopularDestinationWarmupService warmupService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _warmupService = warmupService;
    }

    public async Task<FeaturedDestinationDto> Handle(UpdateFeaturedDestinationCommand request, CancellationToken cancellationToken)
    {
        var destination = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (destination == null)
        {
            throw new NotFoundException("FeaturedDestination", request.Id);
        }

        if (!string.IsNullOrEmpty(request.Data.DestinationName))
            destination.DestinationName = request.Data.DestinationName;

        if (!string.IsNullOrEmpty(request.Data.CountryCode))
            destination.CountryCode = request.Data.CountryCode;

        if (!string.IsNullOrEmpty(request.Data.Country))
            destination.Country = request.Data.Country;

        if (request.Data.Priority.HasValue)
            destination.Priority = request.Data.Priority.Value;

        if (request.Data.Status.HasValue)
            destination.Status = request.Data.Status.Value;

        if (request.Data.Season.HasValue)
            destination.Season = request.Data.Season.Value;

        if (request.Data.Image != null)
            destination.Image = request.Data.Image;

        if (request.Data.Description != null)
            destination.Description = request.Data.Description;

        if (request.Data.ValidFrom.HasValue)
            destination.ValidFrom = request.Data.ValidFrom;

        if (request.Data.ValidUntil.HasValue)
            destination.ValidUntil = request.Data.ValidUntil;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ✅ Admin güncellemesi sonrası: destinasyon için cache warmup'ı tetikle
        await _warmupService.WarmDestinationAsync(destination.DestinationId, cancellationToken);

        return new FeaturedDestinationDto
        {
            Id = destination.Id,
            DestinationId = destination.DestinationId,
            DestinationName = destination.DestinationName,
            CountryCode = destination.CountryCode,
            Country = destination.Country,
            Priority = destination.Priority,
            Status = destination.Status.ToString(),
            Season = destination.Season.ToString(),
            Image = destination.Image,
            Description = destination.Description,
            ValidFrom = destination.ValidFrom,
            ValidUntil = destination.ValidUntil,
            CreatedAt = destination.CreatedAt,
            UpdatedAt = destination.UpdatedAt
        };
    }
}

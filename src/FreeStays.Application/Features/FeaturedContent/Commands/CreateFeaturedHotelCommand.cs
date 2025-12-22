using FluentValidation;
using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Commands;

public record CreateFeaturedHotelCommand : IRequest<FeaturedHotelDto>
{
    public CreateFeaturedHotelDto Data { get; init; } = new();
    public string? CreatedBy { get; init; }
}

public class CreateFeaturedHotelCommandValidator : AbstractValidator<CreateFeaturedHotelCommand>
{
    public CreateFeaturedHotelCommandValidator()
    {
        RuleFor(x => x.Data.HotelId).NotEmpty();
        RuleFor(x => x.Data.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.DiscountPercentage)
            .InclusiveBetween(0, 100)
            .When(x => x.Data.DiscountPercentage.HasValue);
        RuleFor(x => x.Data.ValidFrom)
            .LessThan(x => x.Data.ValidUntil)
            .When(x => x.Data.ValidFrom.HasValue && x.Data.ValidUntil.HasValue);
    }
}

public class CreateFeaturedHotelCommandHandler : IRequestHandler<CreateFeaturedHotelCommand, FeaturedHotelDto>
{
    private readonly IFeaturedHotelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFeaturedHotelCommandHandler(IFeaturedHotelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FeaturedHotelDto> Handle(CreateFeaturedHotelCommand request, CancellationToken cancellationToken)
    {
        // Validate: Same hotel cannot have multiple active campaigns
        if (await _repository.HasActiveByHotelIdAsync(request.Data.HotelId, null, cancellationToken))
        {
            throw new InvalidOperationException($"Hotel {request.Data.HotelId} already has an active featured campaign.");
        }

        var featuredHotel = new FeaturedHotel
        {
            Id = Guid.NewGuid(),
            HotelId = request.Data.HotelId,
            Priority = request.Data.Priority,
            Status = request.Data.Status,
            Season = request.Data.Season,
            Category = request.Data.Category,
            ValidFrom = request.Data.ValidFrom,
            ValidUntil = request.Data.ValidUntil,
            CampaignName = request.Data.CampaignName,
            DiscountPercentage = request.Data.DiscountPercentage,
            CreatedBy = request.CreatedBy
        };

        await _repository.AddAsync(featuredHotel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FeaturedHotelDto
        {
            Id = featuredHotel.Id,
            HotelId = featuredHotel.HotelId,
            Priority = featuredHotel.Priority,
            Status = featuredHotel.Status.ToString(),
            Season = featuredHotel.Season.ToString(),
            Category = featuredHotel.Category?.ToString(),
            ValidFrom = featuredHotel.ValidFrom,
            ValidUntil = featuredHotel.ValidUntil,
            CampaignName = featuredHotel.CampaignName,
            DiscountPercentage = featuredHotel.DiscountPercentage,
            CreatedAt = featuredHotel.CreatedAt,
            UpdatedAt = featuredHotel.UpdatedAt
        };
    }
}

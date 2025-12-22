using FluentValidation;
using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Commands;

public record UpdateFeaturedHotelCommand : IRequest<FeaturedHotelDto>
{
    public Guid Id { get; init; }
    public UpdateFeaturedHotelDto Data { get; init; } = new();
}

public class UpdateFeaturedHotelCommandValidator : AbstractValidator<UpdateFeaturedHotelCommand>
{
    public UpdateFeaturedHotelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data.Priority)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Data.Priority.HasValue);
        RuleFor(x => x.Data.DiscountPercentage)
            .InclusiveBetween(0, 100)
            .When(x => x.Data.DiscountPercentage.HasValue);
    }
}

public class UpdateFeaturedHotelCommandHandler : IRequestHandler<UpdateFeaturedHotelCommand, FeaturedHotelDto>
{
    private readonly IFeaturedHotelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFeaturedHotelCommandHandler(IFeaturedHotelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FeaturedHotelDto> Handle(UpdateFeaturedHotelCommand request, CancellationToken cancellationToken)
    {
        var featuredHotel = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (featuredHotel == null)
        {
            throw new NotFoundException("FeaturedHotel", request.Id);
        }

        if (request.Data.Priority.HasValue)
            featuredHotel.Priority = request.Data.Priority.Value;

        if (request.Data.Status.HasValue)
            featuredHotel.Status = request.Data.Status.Value;

        if (request.Data.Season.HasValue)
            featuredHotel.Season = request.Data.Season.Value;

        if (request.Data.Category.HasValue)
            featuredHotel.Category = request.Data.Category.Value;

        if (request.Data.ValidFrom.HasValue)
            featuredHotel.ValidFrom = request.Data.ValidFrom;

        if (request.Data.ValidUntil.HasValue)
            featuredHotel.ValidUntil = request.Data.ValidUntil;

        if (request.Data.CampaignName != null)
            featuredHotel.CampaignName = request.Data.CampaignName;

        if (request.Data.DiscountPercentage.HasValue)
            featuredHotel.DiscountPercentage = request.Data.DiscountPercentage;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _repository.GetByIdWithHotelAsync(request.Id, cancellationToken);

        return new FeaturedHotelDto
        {
            Id = updated!.Id,
            HotelId = updated.HotelId,
            Priority = updated.Priority,
            Status = updated.Status.ToString(),
            Season = updated.Season.ToString(),
            Category = updated.Category?.ToString(),
            ValidFrom = updated.ValidFrom,
            ValidUntil = updated.ValidUntil,
            CampaignName = updated.CampaignName,
            DiscountPercentage = updated.DiscountPercentage,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }
}

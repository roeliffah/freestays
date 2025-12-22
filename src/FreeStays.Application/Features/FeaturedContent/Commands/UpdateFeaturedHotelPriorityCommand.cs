using FluentValidation;
using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Commands;

public record UpdateFeaturedHotelPriorityCommand : IRequest<Unit>
{
    public Guid Id { get; init; }
    public int Priority { get; init; }
}

public record BulkUpdateFeaturedHotelPriorityCommand : IRequest<Unit>
{
    public BulkPriorityUpdateDto Data { get; init; } = new();
}

public class UpdateFeaturedHotelPriorityCommandValidator : AbstractValidator<UpdateFeaturedHotelPriorityCommand>
{
    public UpdateFeaturedHotelPriorityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
    }
}

public class BulkUpdateFeaturedHotelPriorityCommandValidator : AbstractValidator<BulkUpdateFeaturedHotelPriorityCommand>
{
    public BulkUpdateFeaturedHotelPriorityCommandValidator()
    {
        RuleFor(x => x.Data.Items).NotEmpty();
        RuleForEach(x => x.Data.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEmpty();
            item.RuleFor(i => i.Priority).GreaterThanOrEqualTo(0);
        });
    }
}

public class UpdateFeaturedHotelPriorityCommandHandler : IRequestHandler<UpdateFeaturedHotelPriorityCommand, Unit>
{
    private readonly IFeaturedHotelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFeaturedHotelPriorityCommandHandler(IFeaturedHotelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateFeaturedHotelPriorityCommand request, CancellationToken cancellationToken)
    {
        var priorities = new Dictionary<Guid, int> { { request.Id, request.Priority } };
        await _repository.UpdatePrioritiesAsync(priorities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class BulkUpdateFeaturedHotelPriorityCommandHandler : IRequestHandler<BulkUpdateFeaturedHotelPriorityCommand, Unit>
{
    private readonly IFeaturedHotelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkUpdateFeaturedHotelPriorityCommandHandler(IFeaturedHotelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(BulkUpdateFeaturedHotelPriorityCommand request, CancellationToken cancellationToken)
    {
        var priorities = request.Data.Items.ToDictionary(x => x.Id, x => x.Priority);
        await _repository.UpdatePrioritiesAsync(priorities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

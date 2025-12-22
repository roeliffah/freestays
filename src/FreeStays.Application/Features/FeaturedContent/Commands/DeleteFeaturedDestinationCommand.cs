using FluentValidation;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Commands;

public record DeleteFeaturedDestinationCommand(Guid Id) : IRequest<Unit>;

public class DeleteFeaturedDestinationCommandValidator : AbstractValidator<DeleteFeaturedDestinationCommand>
{
    public DeleteFeaturedDestinationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteFeaturedDestinationCommandHandler : IRequestHandler<DeleteFeaturedDestinationCommand, Unit>
{
    private readonly IFeaturedDestinationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFeaturedDestinationCommandHandler(IFeaturedDestinationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteFeaturedDestinationCommand request, CancellationToken cancellationToken)
    {
        var destination = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (destination == null)
        {
            throw new NotFoundException("FeaturedDestination", request.Id);
        }

        await _repository.DeleteAsync(destination, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

using FluentValidation;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.FeaturedContent.Commands;

public record DeleteFeaturedHotelCommand(Guid Id) : IRequest<Unit>;

public class DeleteFeaturedHotelCommandValidator : AbstractValidator<DeleteFeaturedHotelCommand>
{
    public DeleteFeaturedHotelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteFeaturedHotelCommandHandler : IRequestHandler<DeleteFeaturedHotelCommand, Unit>
{
    private readonly IFeaturedHotelRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFeaturedHotelCommandHandler(IFeaturedHotelRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteFeaturedHotelCommand request, CancellationToken cancellationToken)
    {
        var featuredHotel = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (featuredHotel == null)
        {
            throw new NotFoundException("FeaturedHotel", request.Id);
        }

        await _repository.DeleteAsync(featuredHotel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

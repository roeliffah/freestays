using FluentValidation;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Faqs.Commands;

public record DeleteFaqCommand(Guid Id) : IRequest<Unit>;

public class DeleteFaqCommandValidator : AbstractValidator<DeleteFaqCommand>
{
    public DeleteFaqCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteFaqCommandHandler : IRequestHandler<DeleteFaqCommand, Unit>
{
    private readonly IFaqRepository _faqRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFaqCommandHandler(IFaqRepository faqRepository, IUnitOfWork unitOfWork)
    {
        _faqRepository = faqRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = await _faqRepository.GetByIdAsync(request.Id, cancellationToken);

        if (faq == null)
        {
            throw new NotFoundException("Faq", request.Id);
        }

        await _faqRepository.DeleteAsync(faq, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

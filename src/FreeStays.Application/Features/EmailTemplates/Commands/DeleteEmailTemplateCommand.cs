using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.EmailTemplates.Commands;

public record DeleteEmailTemplateCommand(Guid Id) : IRequest;

public class DeleteEmailTemplateCommandHandler : IRequestHandler<DeleteEmailTemplateCommand>
{
    private readonly IEmailTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmailTemplateCommandHandler(IEmailTemplateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (template == null)
        {
            throw new NotFoundException("EmailTemplate", request.Id.ToString());
        }

        await _repository.DeleteAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

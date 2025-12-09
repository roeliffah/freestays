using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Pages.Commands;

public record DeleteStaticPageCommand(Guid Id) : IRequest<bool>;

public class DeleteStaticPageCommandHandler : IRequestHandler<DeleteStaticPageCommand, bool>
{
    private readonly IStaticPageRepository _pageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteStaticPageCommandHandler(IStaticPageRepository pageRepository, IUnitOfWork unitOfWork)
    {
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteStaticPageCommand request, CancellationToken cancellationToken)
    {
        var page = await _pageRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (page == null)
        {
            throw new NotFoundException("StaticPage", request.Id);
        }

        await _pageRepository.DeleteAsync(page, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

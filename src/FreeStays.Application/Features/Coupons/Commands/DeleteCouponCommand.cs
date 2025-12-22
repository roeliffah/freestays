using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Commands;

public record DeleteCouponCommand(Guid Id) : IRequest<bool>;

public class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, bool>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCouponCommandHandler(
        ICouponRepository couponRepository,
        IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByIdAsync(request.Id, cancellationToken);
        if (coupon == null)
        {
            throw new NotFoundException("Coupon", request.Id);
        }

        await _couponRepository.DeleteAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

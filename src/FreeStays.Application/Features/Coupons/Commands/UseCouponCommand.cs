using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Commands;

public record UseCouponCommand(string Code, Guid? UserId, string? Email) : IRequest<bool>;

public class UseCouponCommandHandler : IRequestHandler<UseCouponCommand, bool>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UseCouponCommandHandler(ICouponRepository couponRepository, IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UseCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (coupon == null)
        {
            throw new NotFoundException("Coupon", request.Code);
        }

        var now = DateTime.UtcNow;
        if (!coupon.IsActive || coupon.ValidFrom > now || coupon.ValidUntil < now)
        {
            throw new Domain.Exceptions.ValidationException("Coupon", "Coupon is not valid");
        }

        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
        {
            throw new Domain.Exceptions.ValidationException("Coupon", "Coupon usage limit reached");
        }

        coupon.UsedCount += 1;
        coupon.UsedAt = now;
        coupon.UsedByUserId = request.UserId;
        coupon.UsedByEmail = request.Email;

        await _couponRepository.UpdateAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

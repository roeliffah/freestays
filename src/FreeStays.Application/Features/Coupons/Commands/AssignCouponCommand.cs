using FreeStays.Application.DTOs.Coupons;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Commands;

public record AssignCouponCommand(Guid Id, Guid? UserId, string? Email) : IRequest<CouponDto>;

public class AssignCouponCommandHandler : IRequestHandler<AssignCouponCommand, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;

    public AssignCouponCommandHandler(
        ICouponRepository couponRepository,
        IUnitOfWork unitOfWork,
        AutoMapper.IMapper mapper)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CouponDto> Handle(AssignCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByIdAsync(request.Id, cancellationToken);
        if (coupon == null)
        {
            throw new NotFoundException("Coupon", request.Id);
        }

        coupon.AssignedUserId = request.UserId;
        coupon.AssignedEmail = request.Email;

        await _couponRepository.UpdateAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CouponDto>(coupon);
    }
}

using FreeStays.Application.DTOs.Coupons;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Commands;

public record UpdateCouponCommand : IRequest<CouponDto>
{
    public Guid Id { get; init; }
    public string? Description { get; init; }
    public decimal? DiscountValue { get; init; }
    public decimal? MinBookingAmount { get; init; }
    public int? MaxUses { get; init; }
    public DateTime? ValidUntil { get; init; }
    public bool? IsActive { get; init; }
}

public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;

    public UpdateCouponCommandHandler(
        ICouponRepository couponRepository,
        IUnitOfWork unitOfWork,
        AutoMapper.IMapper mapper)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CouponDto> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByIdAsync(request.Id, cancellationToken);
        if (coupon == null)
        {
            throw new NotFoundException("Coupon", request.Id);
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
            coupon.Description = request.Description;

        if (request.DiscountValue.HasValue && request.DiscountValue > 0)
            coupon.DiscountValue = request.DiscountValue.Value;

        if (request.MinBookingAmount.HasValue)
            coupon.MinBookingAmount = request.MinBookingAmount;

        if (request.MaxUses.HasValue)
            coupon.MaxUses = request.MaxUses;

        if (request.ValidUntil.HasValue)
            coupon.ValidUntil = request.ValidUntil.Value;

        if (request.IsActive.HasValue)
            coupon.IsActive = request.IsActive.Value;

        await _couponRepository.UpdateAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CouponDto>(coupon);
    }
}

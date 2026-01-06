using FreeStays.Application.DTOs.Coupons;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Queries;

public record ValidateCouponQuery(string Code, decimal? Amount) : IRequest<CouponValidationResultDto>;

public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, CouponValidationResultDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly AutoMapper.IMapper _mapper;

    public ValidateCouponQueryHandler(ICouponRepository couponRepository, AutoMapper.IMapper mapper)
    {
        _couponRepository = couponRepository;
        _mapper = mapper;
    }

    public async Task<CouponValidationResultDto> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (coupon == null)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = "Kupon bulunamadı"
            };
        }

        var now = DateTime.UtcNow;
        if (!coupon.IsActive || coupon.ValidFrom > now || coupon.ValidUntil < now)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = "Kupon geçerli değil"
            };
        }

        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = "Kupon kullanım hakkı doldu"
            };
        }

        decimal discountAmount = 0;
        if (request.Amount.HasValue && request.Amount.Value > 0)
        {
            discountAmount = coupon.DiscountType switch
            {
                Domain.Enums.DiscountType.Percentage => Math.Round(request.Amount.Value * (coupon.DiscountValue / 100m), 2),
                _ => coupon.DiscountValue
            };
        }

        return new CouponValidationResultDto
        {
            IsValid = true,
            Message = "Kupon geçerli",
            Coupon = _mapper.Map<CouponDto>(coupon),
            DiscountAmount = discountAmount
        };
    }
}

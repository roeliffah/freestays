using FreeStays.Application.DTOs.Coupons;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Queries;

public record GetCouponByIdQuery(Guid Id) : IRequest<CouponDto>;

public class GetCouponByIdQueryHandler : IRequestHandler<GetCouponByIdQuery, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly AutoMapper.IMapper _mapper;

    public GetCouponByIdQueryHandler(
        ICouponRepository couponRepository,
        AutoMapper.IMapper mapper)
    {
        _couponRepository = couponRepository;
        _mapper = mapper;
    }

    public async Task<CouponDto> Handle(GetCouponByIdQuery request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByIdAsync(request.Id, cancellationToken);
        if (coupon == null)
        {
            throw new Domain.Exceptions.NotFoundException("Coupon", request.Id);
        }

        return _mapper.Map<CouponDto>(coupon);
    }
}

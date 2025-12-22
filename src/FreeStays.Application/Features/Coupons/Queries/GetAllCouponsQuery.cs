using FreeStays.Application.DTOs.Coupons;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Queries;

public record GetAllCouponsQuery : IRequest<List<CouponDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool? IsActive { get; init; }
}

public record CouponListDto
{
    public List<CouponDto> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

public class GetAllCouponsQueryHandler : IRequestHandler<GetAllCouponsQuery, List<CouponDto>>
{
    private readonly ICouponRepository _couponRepository;
    private readonly AutoMapper.IMapper _mapper;

    public GetAllCouponsQueryHandler(
        ICouponRepository couponRepository,
        AutoMapper.IMapper mapper)
    {
        _couponRepository = couponRepository;
        _mapper = mapper;
    }

    public async Task<List<CouponDto>> Handle(GetAllCouponsQuery request, CancellationToken cancellationToken)
    {
        var allCoupons = (await _couponRepository.GetAllAsync(cancellationToken)).ToList();

        if (request.IsActive.HasValue)
        {
            allCoupons = allCoupons.Where(c => c.IsActive == request.IsActive).ToList();
        }

        var paginatedCoupons = allCoupons
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return _mapper.Map<List<CouponDto>>(paginatedCoupons);
    }
}

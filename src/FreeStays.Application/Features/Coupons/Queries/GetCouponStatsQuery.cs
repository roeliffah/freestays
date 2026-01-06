using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Queries;

public record CouponStatsDto(
    int Total,
    int Active,
    int Passive,
    int Used,
    int Unused,
    int Annual,
    int OneTime,
    decimal TotalRevenueEUR);

public record GetCouponStatsQuery : IRequest<CouponStatsDto>;

public class GetCouponStatsQueryHandler : IRequestHandler<GetCouponStatsQuery, CouponStatsDto>
{
    private readonly ICouponRepository _couponRepository;

    public GetCouponStatsQueryHandler(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<CouponStatsDto> Handle(GetCouponStatsQuery request, CancellationToken cancellationToken)
    {
        var coupons = await _couponRepository.GetAllAsync(cancellationToken);
        var list = coupons.ToList();

        var total = list.Count;
        var active = list.Count(c => c.IsActive);
        var passive = total - active;
        var used = list.Count(c => c.UsedCount > 0 || c.UsedAt != null);
        var unused = total - used;
        var annual = list.Count(c => c.Kind == CouponKind.Annual);
        var oneTime = list.Count(c => c.Kind == CouponKind.OneTime);
        var revenue = list.Sum(c => c.PriceCurrency == "EUR" ? c.PriceAmount : 0m);

        return new CouponStatsDto(total, active, passive, used, unused, annual, oneTime, revenue);
    }
}

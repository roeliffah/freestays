using FluentValidation;
using FreeStays.Application.DTOs.Coupons;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Coupons.Commands;

public record CreateCouponCommand : IRequest<CouponDto>
{
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public CouponKind Kind { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MinBookingAmount { get; init; }
    public int? MaxUses { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime ValidUntil { get; init; }
    public Guid? AssignedUserId { get; init; }
    public string? AssignedEmail { get; init; }
    public string? StripePaymentIntentId { get; init; }
}

public class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(x => x.DiscountValue)
            .GreaterThanOrEqualTo(0);
        RuleFor(x => x.ValidFrom)
            .LessThan(x => x.ValidUntil);
        RuleFor(x => x.Kind)
            .IsInEnum();
    }
}

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;
    private readonly ISiteSettingRepository _siteSettingRepository;

    public CreateCouponCommandHandler(
        ICouponRepository couponRepository,
        IUnitOfWork unitOfWork,
        AutoMapper.IMapper mapper,
        ISiteSettingRepository siteSettingRepository)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _siteSettingRepository = siteSettingRepository;
    }

    public async Task<CouponDto> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var price = await ResolvePriceAsync(request.Kind, cancellationToken);
        var code = GenerateCode(request.Kind, request.Code);
        var maxUses = request.Kind == CouponKind.OneTime ? 1 : request.MaxUses;
        var validUntil = request.ValidUntil != default ? request.ValidUntil : now.AddYears(1);

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code,
            Description = request.Description,
            Kind = request.Kind,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinBookingAmount = request.MinBookingAmount,
            MaxUses = maxUses,
            ValidFrom = request.ValidFrom == default ? now : request.ValidFrom,
            ValidUntil = validUntil,
            IsActive = true,
            UsedCount = 0,
            AssignedUserId = request.AssignedUserId,
            AssignedEmail = request.AssignedEmail,
            PriceAmount = price,
            PriceCurrency = "EUR",
            StripePaymentIntentId = request.StripePaymentIntentId
        };

        await _couponRepository.AddAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CouponDto>(coupon);
    }

    private async Task<decimal> ResolvePriceAsync(CouponKind kind, CancellationToken cancellationToken)
    {
        var key = kind == CouponKind.Annual ? "annualPriceEUR" : "oneTimePriceEUR";
        var setting = await _siteSettingRepository.GetByKeyAsync(key, cancellationToken);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return 0m;
        }

        return decimal.TryParse(setting.Value, out var price) ? price : 0m;
    }

    private static string GenerateCode(CouponKind kind, string existing)
    {
        var cleaned = existing?.Trim().ToUpper() ?? string.Empty;
        var prefix = kind == CouponKind.Annual ? "GOLD-" : "PASS-";
        if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.StartsWith(prefix))
        {
            return cleaned;
        }

        var random = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        return prefix + random;
    }
}

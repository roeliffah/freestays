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
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MinBookingAmount { get; init; }
    public int? MaxUses { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime ValidUntil { get; init; }
}

public class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(x => x.DiscountValue)
            .GreaterThan(0);
        RuleFor(x => x.ValidFrom)
            .LessThan(x => x.ValidUntil);
    }
}

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;

    public CreateCouponCommandHandler(
        ICouponRepository couponRepository,
        IUnitOfWork unitOfWork,
        AutoMapper.IMapper mapper)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CouponDto> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = request.Code.ToUpper(),
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinBookingAmount = request.MinBookingAmount,
            MaxUses = request.MaxUses,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            IsActive = true,
            UsedCount = 0
        };

        await _couponRepository.AddAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CouponDto>(coupon);
    }
}

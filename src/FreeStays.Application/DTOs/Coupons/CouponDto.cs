namespace FreeStays.Application.DTOs.Coupons;

public record CouponDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public int? MaxUses { get; init; }
    public int UsedCount { get; init; }
    public decimal? MinBookingAmount { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime ValidUntil { get; init; }
    public bool IsActive { get; init; }
    public string? Description { get; init; }
}

public record CouponValidationResultDto
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public CouponDto? Coupon { get; init; }
    public decimal DiscountAmount { get; init; }
}

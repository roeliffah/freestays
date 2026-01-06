namespace FreeStays.Application.DTOs.Coupons;

public record CouponDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public DateTime ValidFrom { get; init; }
    public DateTime ValidUntil { get; init; }
    public bool IsActive { get; init; }
    public Guid? AssignedUserId { get; init; }
    public string? AssignedEmail { get; init; }
    public DateTime? UsedAt { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; } = "EUR";
    public string? StripePaymentIntentId { get; init; }
}

public record CouponValidationResultDto
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public CouponDto? Coupon { get; init; }
    public decimal DiscountAmount { get; init; }
}

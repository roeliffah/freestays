using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

[Authorize]
public class CouponsController : BaseApiController
{
    /// <summary>
    /// Kupon kodunu doğrula
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
    {
        // TODO: Implement coupon validation
        return Ok(new 
        { 
            isValid = true,
            code = request.Code,
            discountType = "Percentage",
            discountValue = 10,
            message = "Kupon geçerli"
        });
    }

    /// <summary>
    /// Kuponu uygula
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        // TODO: Implement coupon application
        return Ok(new 
        { 
            success = true,
            originalAmount = request.Amount,
            discountAmount = request.Amount * 0.1m,
            finalAmount = request.Amount * 0.9m,
            message = "Kupon uygulandı"
        });
    }

    /// <summary>
    /// Kullanıcının kuponlarını getir
    /// </summary>
    [HttpGet("my-coupons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCoupons()
    {
        // TODO: Implement get user coupons
        return Ok(new List<object>());
    }
}

public record ValidateCouponRequest(string Code);
public record ApplyCouponRequest(string Code, decimal Amount, string BookingType);

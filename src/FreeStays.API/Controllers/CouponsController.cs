using FreeStays.Application.DTOs.Coupons;
using FreeStays.Application.Features.Coupons.Commands;
using FreeStays.Application.Features.Coupons.Queries;
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
        var result = await Mediator.Send(new ValidateCouponQuery(request.Code, request.Amount));
        return Ok(result);
    }

    /// <summary>
    /// Kuponu uygula
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        await Mediator.Send(new UseCouponCommand(request.Code, request.UserId, request.Email));

        return Ok(new
        {
            success = true,
            code = request.Code,
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

public record ValidateCouponRequest(string Code, decimal? Amount);
public record ApplyCouponRequest(string Code, decimal Amount, string BookingType, Guid? UserId, string? Email);

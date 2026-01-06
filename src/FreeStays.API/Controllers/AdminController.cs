using FreeStays.Application.DTOs.Admin;
using FreeStays.Application.Features.Admin.Commands;
using FreeStays.Application.Features.Admin.Queries;
using FreeStays.Application.Features.Coupons.Commands;
using FreeStays.Application.Features.Coupons.Queries;
using FreeStays.Domain.Enums;
using FreeStays.Infrastructure.BackgroundJobs;
using FreeStays.Infrastructure.Persistence.Context;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin")]
public class AdminController : BaseApiController
{
    #region Dashboard

    /// <summary>
    /// Dashboard istatistikleri, son rezervasyonlar ve popüler destinasyonlar
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await Mediator.Send(new GetDashboardQuery());
        return Ok(result);
    }

    #endregion

    #region Bookings Management

    /// <summary>
    /// Tüm rezervasyonları listele
    /// </summary>
    [HttpGet("bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BookingStatus? status = null,
        [FromQuery] BookingType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await Mediator.Send(new GetAllBookingsQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    /// <summary>
    /// Rezervasyon durumunu güncelle
    /// </summary>
    [HttpPatch("bookings/{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] UpdateBookingStatusRequest request)
    {
        var result = await Mediator.Send(new UpdateBookingStatusCommand
        {
            Id = id,
            Status = request.Status,
            Notes = request.Notes
        });
        return Ok(result);
    }

    #endregion

    #region Coupons Management

    /// <summary>
    /// Tüm kuponları listele
    /// </summary>
    [HttpGet("coupons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCoupons([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isActive = null)
    {
        var result = await Mediator.Send(new GetAllCouponsQuery
        {
            Page = page,
            PageSize = pageSize,
            IsActive = isActive
        });
        return Ok(result);
    }

    /// <summary>
    /// Yeni kupon oluştur
    /// </summary>
    [HttpPost("coupons")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        var result = await Mediator.Send(new CreateCouponCommand
        {
            Code = request.Code,
            Description = request.Description,
            Kind = request.Kind,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinBookingAmount = request.MinimumAmount,
            MaxUses = request.UsageLimit,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            AssignedUserId = request.AssignedUserId,
            AssignedEmail = request.AssignedEmail,
            StripePaymentIntentId = request.StripePaymentIntentId
        });
        return CreatedAtAction(nameof(GetCoupon), new { id = result.Id }, result);
    }

    /// <summary>
    /// Kupon detayları
    /// </summary>
    [HttpGet("coupons/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCoupon(Guid id)
    {
        var result = await Mediator.Send(new GetCouponByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Kupon güncelle
    /// </summary>
    [HttpPut("coupons/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] UpdateCouponRequest request)
    {
        var result = await Mediator.Send(new UpdateCouponCommand
        {
            Id = id,
            Description = request.Description,
            DiscountValue = request.DiscountValue,
            MinBookingAmount = request.MinimumAmount,
            MaxUses = request.UsageLimit,
            ValidUntil = request.ValidUntil,
            IsActive = request.IsActive
        });
        return Ok(result);
    }

    /// <summary>
    /// Kuponu kullanıcıya ata
    /// </summary>
    [HttpPost("coupons/{id}/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignCoupon(Guid id, [FromBody] AssignCouponRequest request)
    {
        var result = await Mediator.Send(new AssignCouponCommand(id, request.UserId, request.Email));
        return Ok(result);
    }

    /// <summary>
    /// Kupon sil
    /// </summary>
    [HttpDelete("coupons/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCoupon(Guid id)
    {
        await Mediator.Send(new DeleteCouponCommand(id));
        return NoContent();
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Dashboard istatistikleri (eski endpoint - deprecated)
    /// </summary>
    [HttpGet("dashboard/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await Mediator.Send(new GetDashboardStatsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Gelir raporu
    /// </summary>
    [HttpGet("reports/revenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        // TODO: Implement revenue report
        return Ok(new
        {
            fromDate = fromDate,
            toDate = toDate,
            totalRevenue = 0m,
            hotelRevenue = 0m,
            flightRevenue = 0m,
            carRevenue = 0m
        });
    }

    /// <summary>
    /// Kupon istatistikleri
    /// </summary>
    [HttpGet("coupons/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCouponStats()
    {
        var result = await Mediator.Send(new GetCouponStatsQuery());
        return Ok(result);
    }

    #endregion

    #region External Services Configuration

    /// <summary>
    /// Harici servis yapılandırmalarını getir
    /// </summary>
    [HttpGet("services")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExternalServices()
    {
        // TODO: Implement get external services
        return Ok(new List<object>());
    }

    /// <summary>
    /// Harici servis yapılandırmasını güncelle
    /// </summary>
    [HttpPut("services/{serviceId}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExternalService(Guid serviceId, [FromBody] UpdateExternalServiceRequest request)
    {
        // TODO: Implement update external service
        return Ok(new { message = "Servis yapılandırması güncellendi." });
    }

    /// <summary>
    /// SunHotels statik veri senkronizasyonunu başlat
    /// </summary>
    [HttpPost("services/sunhotels/sync")]
    [AllowAnonymous] // TODO: Remove after testing
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SyncSunHotelsData()
    {
        BackgroundJob.Enqueue<SunHotelsStaticDataSyncJob>(job => job.SyncAllStaticDataAsync());
        return Ok(new { message = "SunHotels veri senkronizasyonu başlatıldı." });
    }

    #endregion

    #region Job History

    /// <summary>
    /// Job geçmişini listele
    /// </summary>
    [HttpGet("jobs/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? jobType = null,
        [FromServices] FreeStaysDbContext dbContext = null!)
    {
        var query = dbContext.JobHistories.AsQueryable();

        if (!string.IsNullOrEmpty(jobType))
        {
            query = query.Where(j => j.JobType == jobType);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id,
                j.JobType,
                j.Status,
                j.StartTime,
                j.EndTime,
                j.DurationSeconds,
                j.Message,
                j.Details
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            items
        });
    }

    /// <summary>
    /// Job detayını getir
    /// </summary>
    [HttpGet("jobs/history/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobHistoryById(
        Guid id,
        [FromServices] FreeStaysDbContext dbContext = null!)
    {
        var job = await dbContext.JobHistories
            .Where(j => j.Id == id)
            .Select(j => new
            {
                j.Id,
                j.JobType,
                j.Status,
                j.StartTime,
                j.EndTime,
                j.DurationSeconds,
                j.Message,
                j.Details,
                j.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (job == null)
            return NotFound(new { message = "Job history not found" });

        return Ok(job);
    }

    #endregion
}

// Request DTOs
public record CreateUserRequest(
    string Email,
    string Password,
    string Name,
    string? Phone,
    UserRole Role,
    string? Notes);

public record UpdateUserRequest(
    string? Name,
    string? Phone,
    UserRole? Role,
    string? Notes,
    bool? IsBlocked);

public record UpdateUserStatusRequest(bool IsActive, string? Reason);
public record UpdateBookingStatusRequest(BookingStatus Status, string? Notes);
public record CreateCouponRequest(
    string Code,
    string Description,
    CouponKind Kind,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinimumAmount,
    decimal? MaximumDiscount,
    int? UsageLimit,
    DateTime ValidFrom,
    DateTime ValidUntil,
    Guid? AssignedUserId,
    string? AssignedEmail,
    string? StripePaymentIntentId);
public record UpdateCouponRequest(
    string? Description,
    decimal? DiscountValue,
    decimal? MinimumAmount,
    decimal? MaximumDiscount,
    int? UsageLimit,
    DateTime? ValidUntil,
    bool? IsActive);
public record AssignCouponRequest(Guid? UserId, string? Email);
public record UpdateExternalServiceRequest(
    string? BaseUrl,
    string? Username,
    string? Password,
    string? ApiKey,
    bool IsActive);
using FreeStays.Application.DTOs.FeaturedContent;
using FreeStays.Application.Features.FeaturedContent.Commands;
using FreeStays.Application.Features.FeaturedContent.Queries;
using FreeStays.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]  // ✅ Buna çevirin
[Route("api/v1/admin/featured-content")]
public class AdminFeaturedContentController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminFeaturedContentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region Featured Hotels

    /// <summary>
    /// Get all featured hotels (admin)
    /// </summary>
    [HttpGet("hotels")]
    public async Task<IActionResult> GetAllFeaturedHotels(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? season = null,
        [FromQuery] string? category = null)
    {
        FeaturedContentStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FeaturedContentStatus>(status, true, out var st))
            statusEnum = st;

        Season? seasonEnum = null;
        if (!string.IsNullOrEmpty(season) && Enum.TryParse<Season>(season, true, out var se))
            seasonEnum = se;

        HotelCategory? categoryEnum = null;
        if (!string.IsNullOrEmpty(category) && Enum.TryParse<HotelCategory>(category, true, out var c))
            categoryEnum = c;

        var query = new GetAllFeaturedHotelsQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = statusEnum,
            Season = seasonEnum,
            Category = categoryEnum
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create new featured hotel
    /// </summary>
    [HttpPost("hotels")]
    public async Task<IActionResult> CreateFeaturedHotel([FromBody] CreateFeaturedHotelDto request)
    {
        var command = new CreateFeaturedHotelCommand
        {
            Data = request,
            CreatedBy = User.Identity?.Name
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAllFeaturedHotels), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update featured hotel
    /// </summary>
    [HttpPut("hotels/{id}")]
    public async Task<IActionResult> UpdateFeaturedHotel(Guid id, [FromBody] UpdateFeaturedHotelDto request)
    {
        var command = new UpdateFeaturedHotelCommand
        {
            Id = id,
            Data = request
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete featured hotel
    /// </summary>
    [HttpDelete("hotels/{id}")]
    public async Task<IActionResult> DeleteFeaturedHotel(Guid id)
    {
        var command = new DeleteFeaturedHotelCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Update featured hotel priority
    /// </summary>
    [HttpPatch("hotels/{id}/priority")]
    public async Task<IActionResult> UpdateFeaturedHotelPriority(Guid id, [FromBody] PriorityUpdateRequest request)
    {
        var command = new UpdateFeaturedHotelPriorityCommand
        {
            Id = id,
            Priority = request.Priority
        };

        await _mediator.Send(command);
        return Ok();
    }

    /// <summary>
    /// Bulk update featured hotel priorities (for drag & drop)
    /// </summary>
    [HttpPatch("hotels/bulk-priority")]
    public async Task<IActionResult> BulkUpdateFeaturedHotelPriority([FromBody] BulkPriorityUpdateDto request)
    {
        var command = new BulkUpdateFeaturedHotelPriorityCommand
        {
            Data = request
        };

        await _mediator.Send(command);
        return Ok();
    }

    #endregion

    #region Featured Destinations

    /// <summary>
    /// Get all featured destinations (admin)
    /// </summary>
    [HttpGet("destinations")]
    public async Task<IActionResult> GetAllFeaturedDestinations(
        [FromQuery] string? status = null,
        [FromQuery] string? season = null)
    {
        FeaturedContentStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FeaturedContentStatus>(status, true, out var st))
            statusEnum = st;

        Season? seasonEnum = null;
        if (!string.IsNullOrEmpty(season) && Enum.TryParse<Season>(season, true, out var se))
            seasonEnum = se;

        var query = new GetAllFeaturedDestinationsQuery
        {
            Status = statusEnum,
            Season = seasonEnum
        };

        var result = await _mediator.Send(query);
        return Ok(new { items = result });
    }

    /// <summary>
    /// Create new featured destination
    /// </summary>
    [HttpPost("destinations")]
    public async Task<IActionResult> CreateFeaturedDestination([FromBody] CreateFeaturedDestinationDto request)
    {
        var command = new CreateFeaturedDestinationCommand
        {
            Data = request
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAllFeaturedDestinations), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update featured destination
    /// </summary>
    [HttpPut("destinations/{id}")]
    public async Task<IActionResult> UpdateFeaturedDestination(Guid id, [FromBody] UpdateFeaturedDestinationDto request)
    {
        var command = new UpdateFeaturedDestinationCommand
        {
            Id = id,
            Data = request
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete featured destination
    /// </summary>
    [HttpDelete("destinations/{id}")]
    public async Task<IActionResult> DeleteFeaturedDestination(Guid id)
    {
        var command = new DeleteFeaturedDestinationCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }

    #endregion
}

public class PriorityUpdateRequest
{
    public int Priority { get; set; }
}

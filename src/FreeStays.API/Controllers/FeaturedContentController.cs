using FreeStays.Application.Features.FeaturedContent.Queries;
using FreeStays.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

[AllowAnonymous]
public class FeaturedContentController : BaseApiController
{
    private readonly IMediator _mediator;

    public FeaturedContentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get active featured hotels for frontend
    /// </summary>
    /// <param name="count">Number of hotels to return (default: 10)</param>
    /// <param name="season">Season filter (optional)</param>
    /// <param name="category">Category filter (optional)</param>
    [HttpGet("hotels")]
    public async Task<IActionResult> GetActiveFeaturedHotels(
        [FromQuery] int count = 10,
        [FromQuery] string? season = null,
        [FromQuery] string? category = null)
    {
        Season? seasonEnum = null;
        if (!string.IsNullOrEmpty(season) && Enum.TryParse<Season>(season, true, out var s))
            seasonEnum = s;

        HotelCategory? categoryEnum = null;
        if (!string.IsNullOrEmpty(category) && Enum.TryParse<HotelCategory>(category, true, out var c))
            categoryEnum = c;

        var query = new GetActiveFeaturedHotelsQuery
        {
            Count = count,
            Season = seasonEnum,
            Category = categoryEnum
        };

        var result = await _mediator.Send(query);
        return Ok(new { data = result });
    }

    /// <summary>
    /// Get active featured destinations for frontend
    /// </summary>
    /// <param name="count">Number of destinations to return (default: 10)</param>
    /// <param name="season">Season filter (optional)</param>
    [HttpGet("destinations")]
    public async Task<IActionResult> GetActiveFeaturedDestinations(
        [FromQuery] int count = 10,
        [FromQuery] string? season = null)
    {
        Season? seasonEnum = null;
        if (!string.IsNullOrEmpty(season) && Enum.TryParse<Season>(season, true, out var s))
            seasonEnum = s;

        var query = new GetActiveFeaturedDestinationsQuery
        {
            Count = count,
            Season = seasonEnum
        };

        var result = await _mediator.Send(query);
        return Ok(new { data = result });
    }
}

using FreeStays.Application.Features.Faqs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

[AllowAnonymous]
public class FaqsController : BaseApiController
{
    private readonly IMediator _mediator;

    public FaqsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all active FAQs for frontend
    /// </summary>
    /// <param name="locale">Optional locale filter (e.g., 'tr', 'en')</param>
    /// <param name="category">Optional category filter</param>
    [HttpGet]
    public async Task<IActionResult> GetActiveFaqs([FromQuery] string? locale = null, [FromQuery] string? category = null)
    {
        var query = new GetActiveFaqsQuery(locale, category);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get FAQ by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFaqById(Guid id)
    {
        var query = new GetFaqByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

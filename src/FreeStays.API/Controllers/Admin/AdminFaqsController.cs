using FreeStays.Application.DTOs.Faqs;
using FreeStays.Application.Features.Faqs.Commands;
using FreeStays.Application.Features.Faqs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]  // ✅ Buna çevirin
[Route("api/v1/admin/faqs")]
public class AdminFaqsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminFaqsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all FAQs (including inactive)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllFaqs()
    {
        var query = new GetAllFaqsQuery();
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

    /// <summary>
    /// Create new FAQ
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateFaq([FromBody] CreateFaqRequest request)
    {
        var command = new CreateFaqCommand
        {
            Order = request.Order,
            IsActive = request.IsActive,
            Category = request.Category,
            Translations = request.Translations
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetFaqById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update FAQ
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFaq(Guid id, [FromBody] UpdateFaqRequest request)
    {
        var command = new UpdateFaqCommand
        {
            Id = id,
            Order = request.Order,
            IsActive = request.IsActive,
            Category = request.Category,
            Translations = request.Translations
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete FAQ
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFaq(Guid id)
    {
        var command = new DeleteFaqCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}

public class CreateFaqRequest
{
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Category { get; set; }
    public List<CreateFaqTranslationDto> Translations { get; set; } = new();
}

public class UpdateFaqRequest
{
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public string? Category { get; set; }
    public List<CreateFaqTranslationDto> Translations { get; set; } = new();
}

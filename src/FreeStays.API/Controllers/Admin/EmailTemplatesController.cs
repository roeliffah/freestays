using FreeStays.Application.DTOs.EmailTemplates;
using FreeStays.Application.Features.EmailTemplates.Commands;
using FreeStays.Application.Features.EmailTemplates.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/email-templates")]
public class EmailTemplatesController : BaseApiController
{
    /// <summary>
    /// Tüm e-posta şablonlarını listele
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var result = await Mediator.Send(new GetAllEmailTemplatesQuery { IsActive = isActive });
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir e-posta şablonunu getir (ID ile)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetEmailTemplateByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir e-posta şablonunu getir (Code ile) - Dil parametresi ile
    /// </summary>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code, [FromQuery] string locale = "tr")
    {
        var result = await Mediator.Send(new GetEmailTemplateByCodeQuery(code, locale));
        return Ok(result);
    }

    /// <summary>
    /// Yeni e-posta şablonu oluştur
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEmailTemplateRequest request)
    {
        var command = new CreateEmailTemplateCommand
        {
            Code = request.Code,
            Subject = request.Subject,
            Body = request.Body,
            Variables = request.Variables,
            IsActive = request.IsActive
        };

        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// E-posta şablonunu güncelle
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmailTemplateRequest request)
    {
        var command = new UpdateEmailTemplateCommand
        {
            Id = id,
            Subject = request.Subject,
            Body = request.Body,
            Variables = request.Variables,
            IsActive = request.IsActive
        };

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// E-posta şablonunu sil
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteEmailTemplateCommand(id));
        return NoContent();
    }

    /// <summary>
    /// E-posta şablonunu aktif/pasif yap
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await Mediator.Send(new ToggleEmailTemplateStatusCommand(id));
        return Ok(result);
    }
}

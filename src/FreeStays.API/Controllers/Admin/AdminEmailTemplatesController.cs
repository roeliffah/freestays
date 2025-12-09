using FreeStays.Application.Features.EmailTemplates.Commands;
using FreeStays.Application.Features.EmailTemplates.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/email-templates")]
public class AdminEmailTemplatesController : BaseApiController
{
    /// <summary>
    /// Tüm e-posta şablonlarını listele
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailTemplates([FromQuery] bool? isActive = null)
    {
        var templates = await Mediator.Send(new GetEmailTemplatesQuery());
        
        if (isActive.HasValue)
        {
            templates = templates.Where(t => t.IsActive == isActive.Value).ToList();
        }
        
        return Ok(new { items = templates });
    }

    /// <summary>
    /// E-posta şablonu detayları
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailTemplate(string code)
    {
        var template = await Mediator.Send(new GetEmailTemplateByCodeQuery(code));
        return Ok(template);
    }

    /// <summary>
    /// E-posta şablonunu güncelle
    /// </summary>
    [HttpPut("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmailTemplate(string code, [FromBody] UpdateEmailTemplateRequest request)
    {
        var result = await Mediator.Send(new UpdateEmailTemplateCommand
        {
            Code = code,
            Subject = System.Text.Json.JsonSerializer.Serialize(request.Subject ?? new Dictionary<string, string>()),
            Body = System.Text.Json.JsonSerializer.Serialize(request.Body ?? new Dictionary<string, string>()),
            Variables = request.Variables != null ? System.Text.Json.JsonSerializer.Serialize(request.Variables) : null,
            IsActive = request.IsActive ?? true
        });
        
        return Ok(result);
    }

    /// <summary>
    /// Test e-postası gönder
    /// </summary>
    [HttpPost("{code}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendTestEmail(string code, [FromBody] SendTestEmailRequest request)
    {
        await Mediator.Send(new SendTestEmailCommand
        {
            Code = code,
            To = request.Email,
            Variables = request.TestVariables
        });
        
        return Ok(new { message = $"Test e-postası {request.Email} adresine gönderildi." });
    }
}

public record UpdateEmailTemplateRequest(
    Dictionary<string, string>? Subject,
    Dictionary<string, string>? Body,
    string[]? Variables,
    bool? IsActive);

public record SendTestEmailRequest(
    string Email,
    string Locale,
    Dictionary<string, string>? TestVariables);

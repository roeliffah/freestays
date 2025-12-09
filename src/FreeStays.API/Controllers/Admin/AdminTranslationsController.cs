using FreeStays.Application.Features.Translations.Commands;
using FreeStays.Application.Features.Translations.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/translations")]
public class AdminTranslationsController : BaseApiController
{
    /// <summary>
    /// Tüm çevirileri getir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTranslations([FromQuery] string locale = "tr", [FromQuery] string? ns = null)
    {
        if (!string.IsNullOrEmpty(ns))
        {
            var result = await Mediator.Send(new GetTranslationsByNamespaceQuery(locale, ns));
            return Ok(new { items = result.Translations, locales = new[] { "tr", "en", "de", "fr" } });
        }
        
        var translations = await Mediator.Send(new GetAllTranslationsQuery(locale));
        return Ok(new { items = translations, locales = new[] { "tr", "en", "de", "fr" } });
    }

    /// <summary>
    /// Belirli bir dilin çevirilerini getir
    /// </summary>
    [HttpGet("{locale}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTranslationsByLocale(string locale, [FromQuery] string? ns = null)
    {
        if (!string.IsNullOrEmpty(ns))
        {
            var result = await Mediator.Send(new GetTranslationsByNamespaceQuery(locale, ns));
            return Ok(new Dictionary<string, Dictionary<string, string>>
            {
                [ns] = result.Translations
            });
        }
        
        var translations = await Mediator.Send(new GetAllTranslationsQuery(locale));
        var grouped = translations
            .GroupBy(t => t.Namespace)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Key, t => t.Value));
        
        return Ok(grouped);
    }

    /// <summary>
    /// Belirli bir dilin çevirilerini güncelle
    /// </summary>
    [HttpPut("{locale}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTranslations(string locale, [FromBody] UpdateTranslationsRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        foreach (var ns in request.Translations)
        {
            foreach (var translation in ns.Value)
            {
                await Mediator.Send(new UpdateTranslationCommand
                {
                    Locale = locale,
                    Namespace = ns.Key,
                    Key = translation.Key,
                    Value = translation.Value,
                    UpdatedBy = userId
                });
            }
        }
        
        return Ok(new { message = "Çeviriler güncellendi." });
    }

    /// <summary>
    /// Yeni çeviri anahtarı ekle
    /// </summary>
    [HttpPost("{locale}/key")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddTranslationKey(string locale, [FromBody] AddTranslationKeyRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var result = await Mediator.Send(new CreateTranslationCommand
        {
            Locale = locale,
            Namespace = request.Namespace,
            Key = request.Key,
            Value = request.Value,
            UpdatedBy = userId
        });
        
        return Created($"/api/v1/admin/translations/{locale}", result);
    }
}

public record UpdateTranslationsRequest(Dictionary<string, Dictionary<string, string>> Translations);
public record AddTranslationKeyRequest(string Namespace, string Key, string Value);

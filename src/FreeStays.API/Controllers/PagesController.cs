using FreeStays.Application.Features.Pages.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

[AllowAnonymous]
public class PagesController : BaseApiController
{
    /// <summary>
    /// Statik sayfa içeriğini getir (varsayılan dil)
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPage(string slug, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null)
    {
        var locale = ParseLocale(acceptLanguage) ?? "tr";
        var result = await Mediator.Send(new GetStaticPageBySlugQuery(slug, locale));
        
        if (result == null)
            return NotFound(new { message = "Page not found" });
        
        return Ok(result);
    }

    /// <summary>
    /// Statik sayfa içeriğini belirli bir dil için getir
    /// </summary>
    [HttpGet("{slug}/{locale}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageByLocale(string slug, string locale)
    {
        var result = await Mediator.Send(new GetStaticPageBySlugQuery(slug, locale));
        
        if (result == null)
            return NotFound(new { message = "Page not found" });
        
        return Ok(result);
    }

    private static string? ParseLocale(string? acceptLanguage)
    {
        if (string.IsNullOrEmpty(acceptLanguage))
            return null;

        var parts = acceptLanguage.Split(',');
        if (parts.Length > 0)
        {
            var firstLocale = parts[0].Split(';')[0].Trim();
            if (firstLocale.Contains('-'))
            {
                firstLocale = firstLocale.Split('-')[0];
            }
            return firstLocale.ToLowerInvariant();
        }
        return null;
    }
}

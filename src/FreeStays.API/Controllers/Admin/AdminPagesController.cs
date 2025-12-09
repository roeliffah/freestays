using FreeStays.Application.DTOs.Pages;
using FreeStays.Application.Features.Pages.Commands;
using FreeStays.Application.Features.Pages.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/pages")]
public class AdminPagesController : BaseApiController
{
    /// <summary>
    /// Tüm statik sayfaları listele
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPages([FromQuery] bool? isActive = null)
    {
        var pages = await Mediator.Send(new GetAllStaticPagesQuery());
        
        if (isActive.HasValue)
        {
            pages = pages.Where(p => p.IsActive == isActive.Value).ToList();
        }
        
        return Ok(new { items = pages });
    }

    /// <summary>
    /// Sayfa detaylarını slug ile getir
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPage(string slug)
    {
        var pages = await Mediator.Send(new GetAllStaticPagesQuery());
        var page = pages.FirstOrDefault(p => p.Slug == slug);
        
        if (page == null)
            return NotFound(new { message = "Page not found" });
        
        return Ok(page);
    }

    /// <summary>
    /// Yeni sayfa oluştur
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request)
    {
        var command = new CreateStaticPageCommand
        {
            Slug = request.Slug,
            IsActive = request.IsActive,
            Translations = request.Translations.Select(t => new CreateStaticPageTranslationDto
            {
                Locale = t.Locale,
                Title = t.Title,
                Content = t.Content,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription
            }).ToList()
        };
        
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetPage), new { slug = result.Slug }, result);
    }

    /// <summary>
    /// Sayfayı güncelle
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePage(Guid id, [FromBody] UpdatePageRequest request)
    {
        var command = new UpdateStaticPageCommand
        {
            Id = id,
            Slug = request.Slug ?? string.Empty,
            IsActive = request.IsActive ?? true,
            Translations = request.Translations?.Select(t => new CreateStaticPageTranslationDto
            {
                Locale = t.Locale,
                Title = t.Title,
                Content = t.Content,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription
            }).ToList() ?? new List<CreateStaticPageTranslationDto>()
        };
        
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Sayfayı sil
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePage(Guid id)
    {
        await Mediator.Send(new DeleteStaticPageCommand(id));
        return NoContent();
    }
}

public record CreatePageRequest(
    string Slug,
    bool IsActive,
    List<PageTranslationRequest> Translations);

public record UpdatePageRequest(
    string? Slug,
    bool? IsActive,
    List<PageTranslationRequest>? Translations);

public record PageTranslationRequest(
    string Locale,
    string Title,
    string Content,
    string? MetaTitle,
    string? MetaDescription);

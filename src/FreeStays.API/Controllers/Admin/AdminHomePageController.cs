using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.HomePage;
using FreeStays.Domain.Entities;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers.Admin;

/// <summary>
/// Homepage Section Management (Admin)
/// </summary>
[ApiController]
[Route("api/v1/admin/homepage")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminHomePageController : ControllerBase
{
    private readonly FreeStaysDbContext _context;
    private readonly ISunHotelsCacheService _cacheService;
    private readonly ILogger<AdminHomePageController> _logger;

    public AdminHomePageController(
        FreeStaysDbContext context,
        ISunHotelsCacheService cacheService,
        ILogger<AdminHomePageController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm homepage section'larını getir (Admin)
    /// </summary>
    [HttpGet("sections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSections(CancellationToken cancellationToken)
    {
        try
        {
            var sections = await _context.HomePageSections
                .Include(s => s.Translations)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync(cancellationToken);

            var result = sections.Select(s => new HomePageSectionDto
            {
                Id = s.Id,
                SectionType = s.SectionType,
                IsActive = s.IsActive,
                DisplayOrder = s.DisplayOrder,
                Configuration = JsonSerializer.Deserialize<object>(s.Configuration),
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt ?? s.CreatedAt
            }).ToList();

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching homepage sections");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Yeni section oluştur
    /// </summary>
    [HttpPost("sections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSection([FromBody] CreateHomePageSectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var validTypes = new[] { "hero", "room-types", "features", "popular-hotels",
                "popular-destinations", "popular-countries","themed-hotels", "romantic-tours", "campaign-banner",
                "travel-cta", "final-cta", "custom-html" };

            if (!validTypes.Contains(request.SectionType))
            {
                return BadRequest(new { success = false, message = "Invalid section type" });
            }

            var section = new HomePageSection
            {
                SectionType = request.SectionType,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                Configuration = request.Configuration
            };

            _context.HomePageSections.Add(section);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Section created successfully",
                data = new { section.Id, section.SectionType, section.DisplayOrder }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating homepage section");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section güncelle
    /// </summary>
    [HttpPut("sections/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSection(Guid id, [FromBody] UpdateHomePageSectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var section = await _context.HomePageSections.FindAsync(new object[] { id }, cancellationToken);
            if (section == null)
            {
                return NotFound(new { success = false, message = "Section not found" });
            }

            section.IsActive = request.IsActive;
            section.DisplayOrder = request.DisplayOrder;
            section.Configuration = request.Configuration;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Section updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating homepage section");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section sil
    /// </summary>
    [HttpDelete("sections/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSection(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var section = await _context.HomePageSections.FindAsync(new object[] { id }, cancellationToken);
            if (section == null)
            {
                return NotFound(new { success = false, message = "Section not found" });
            }

            _context.HomePageSections.Remove(section);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Section deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting homepage section");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section'ları yeniden sırala
    /// </summary>
    [HttpPatch("sections/reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderSections([FromBody] ReorderSectionsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var item in request.SectionOrders)
            {
                var section = await _context.HomePageSections.FindAsync(new object[] { item.Id }, cancellationToken);
                if (section != null)
                {
                    section.DisplayOrder = item.DisplayOrder;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Sections reordered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering sections");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section aktif/pasif durumunu değiştir
    /// </summary>
    [HttpPatch("sections/{id}/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleSection(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var section = await _context.HomePageSections.FindAsync(new object[] { id }, cancellationToken);
            if (section == null)
            {
                return NotFound(new { success = false, message = "Section not found" });
            }

            section.IsActive = !section.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Section status updated",
                data = new { section.Id, section.IsActive }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling section");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section çevirilerini getir
    /// </summary>
    [HttpGet("sections/{id}/translations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectionTranslations(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var section = await _context.HomePageSections
                .Include(s => s.Translations)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (section == null)
            {
                return NotFound(new { success = false, message = "Section not found" });
            }

            var translations = section.Translations
                .ToDictionary(
                    t => t.Locale,
                    t => new TranslationDto
                    {
                        Locale = t.Locale,
                        Title = t.Title,
                        Subtitle = t.Subtitle
                    }
                );

            return Ok(new { success = true, data = translations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching section translations");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section çevirilerini güncelle
    /// </summary>
    [HttpPost("sections/{id}/translations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSectionTranslations(Guid id, [FromBody] UpdateSectionTranslationsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var section = await _context.HomePageSections
                .Include(s => s.Translations)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (section == null)
            {
                return NotFound(new { success = false, message = "Section not found" });
            }

            // Mevcut çevirileri temizle
            _context.HomePageSectionTranslations.RemoveRange(section.Translations);

            // Yeni çevirileri ekle
            foreach (var (locale, translation) in request.Translations)
            {
                section.Translations.Add(new HomePageSectionTranslation
                {
                    SectionId = id,
                    Locale = locale,
                    Title = translation.Title,
                    Subtitle = translation.Subtitle
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Translations updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating section translations");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section'a ait otelleri getir
    /// </summary>
    [HttpGet("sections/{id}/hotels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionHotels(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var sectionHotels = await _context.HomePageSectionHotels
                .Where(sh => sh.SectionId == id)
                .OrderBy(sh => sh.DisplayOrder)
                .ToListAsync(cancellationToken);

            var result = new List<SectionHotelDto>();

            foreach (var sh in sectionHotels)
            {
                var hotel = await _cacheService.GetHotelByIdAsync(int.Parse(sh.HotelId), "en", cancellationToken);
                result.Add(new SectionHotelDto
                {
                    Id = sh.Id,
                    HotelId = sh.HotelId,
                    DisplayOrder = sh.DisplayOrder,
                    HotelDetails = hotel != null ? new
                    {
                        hotelName = hotel.Name,
                        city = hotel.City,
                        country = hotel.Country,
                        stars = hotel.Category,
                        image = JsonSerializer.Deserialize<List<string>>(hotel.ImageUrls)?.FirstOrDefault()
                    } : null
                });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching section hotels");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section'a ait otelleri güncelle
    /// </summary>
    [HttpPost("sections/{id}/hotels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSectionHotels(Guid id, [FromBody] UpdateSectionHotelsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Mevcut otelleri sil
            var existingHotels = await _context.HomePageSectionHotels
                .Where(sh => sh.SectionId == id)
                .ToListAsync(cancellationToken);
            _context.HomePageSectionHotels.RemoveRange(existingHotels);

            // Yeni otelleri ekle
            foreach (var hotel in request.Hotels)
            {
                _context.HomePageSectionHotels.Add(new HomePageSectionHotel
                {
                    SectionId = id,
                    HotelId = hotel.HotelId,
                    DisplayOrder = hotel.DisplayOrder
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Section hotels updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating section hotels");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section'a ait destinasyonları getir
    /// </summary>
    [HttpGet("sections/{id}/destinations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionDestinations(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var sectionDestinations = await _context.HomePageSectionDestinations
                .Where(sd => sd.SectionId == id)
                .OrderBy(sd => sd.DisplayOrder)
                .ToListAsync(cancellationToken);

            var result = new List<SectionDestinationDto>();

            foreach (var sd in sectionDestinations)
            {
                var destination = await _cacheService.GetDestinationByIdAsync(int.Parse(sd.DestinationId), cancellationToken);
                result.Add(new SectionDestinationDto
                {
                    Id = sd.Id,
                    DestinationId = sd.DestinationId,
                    DisplayOrder = sd.DisplayOrder,
                    DestinationDetails = destination != null ? new
                    {
                        name = destination.Name,
                        country = destination.Country,
                        countryCode = destination.CountryCode
                    } : null
                });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching section destinations");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Section'a ait destinasyonları güncelle
    /// </summary>
    [HttpPost("sections/{id}/destinations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSectionDestinations(Guid id, [FromBody] UpdateSectionDestinationsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Mevcut destinasyonları sil
            var existingDestinations = await _context.HomePageSectionDestinations
                .Where(sd => sd.SectionId == id)
                .ToListAsync(cancellationToken);
            _context.HomePageSectionDestinations.RemoveRange(existingDestinations);

            // Yeni destinasyonları ekle
            foreach (var destination in request.Destinations)
            {
                _context.HomePageSectionDestinations.Add(new HomePageSectionDestination
                {
                    SectionId = id,
                    DestinationId = destination.DestinationId,
                    DisplayOrder = destination.DisplayOrder
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, message = "Section destinations updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating section destinations");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}

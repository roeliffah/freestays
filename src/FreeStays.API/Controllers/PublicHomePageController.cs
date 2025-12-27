using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers;

/// <summary>
/// Public Homepage Sections API - Frontend için optimize edilmiş, hızlı endpoint
/// </summary>
[ApiController]
[Route("api/v1/public/homepage")]
[AllowAnonymous]
public class PublicHomePageController : ControllerBase
{
    private readonly FreeStaysDbContext _context;
    private readonly ISunHotelsCacheService _cacheService;
    private readonly ILogger<PublicHomePageController> _logger;

    public PublicHomePageController(
        FreeStaysDbContext context,
        ISunHotelsCacheService cacheService,
        ILogger<PublicHomePageController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Aktif homepage section'larını getir (Public - Frontend için)
    /// </summary>
    /// <param name="locale">Dil kodu (tr, en, de, fr, etc.)</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("sections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSections(
        [FromQuery] string locale = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sections = await _context.HomePageSections
                .Where(s => s.IsActive)
                .Include(s => s.Translations.Where(t => t.Locale == locale))
                .Include(s => s.Hotels)
                .Include(s => s.Destinations)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync(cancellationToken);

            var result = new List<object>();

            foreach (var section in sections)
            {
                var translation = section.Translations.FirstOrDefault();
                var config = JsonSerializer.Deserialize<object>(section.Configuration);

                var sectionData = new Dictionary<string, object?>
                {
                    ["id"] = section.Id,
                    ["sectionType"] = section.SectionType,
                    ["title"] = translation?.Title,
                    ["subtitle"] = translation?.Subtitle,
                    ["displayOrder"] = section.DisplayOrder,
                    ["configuration"] = config
                };

                // Eğer section hotel veya destination içeriyorsa, bunları da ekle
                if (section.SectionType == "popular-hotels" || section.SectionType == "romantic-tours")
                {
                    var hotels = await GetSectionHotelsData(section.Hotels, locale, cancellationToken);
                    sectionData["hotels"] = hotels;
                }
                else if (section.SectionType == "popular-destinations")
                {
                    var destinations = await GetSectionDestinationsData(section.Destinations, cancellationToken);
                    sectionData["destinations"] = destinations;
                }

                result.Add(sectionData);
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active homepage sections");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Specific section detaylarını getir
    /// </summary>
    [HttpGet("sections/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectionById(
        Guid id,
        [FromQuery] string locale = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var section = await _context.HomePageSections
                .Where(s => s.Id == id && s.IsActive)
                .Include(s => s.Translations.Where(t => t.Locale == locale))
                .Include(s => s.Hotels)
                .Include(s => s.Destinations)
                .FirstOrDefaultAsync(cancellationToken);

            if (section == null)
            {
                return NotFound(new { success = false, message = "Section not found" });
            }

            var translation = section.Translations.FirstOrDefault();
            var config = JsonSerializer.Deserialize<object>(section.Configuration);

            var sectionData = new Dictionary<string, object?>
            {
                ["id"] = section.Id,
                ["sectionType"] = section.SectionType,
                ["title"] = translation?.Title,
                ["subtitle"] = translation?.Subtitle,
                ["displayOrder"] = section.DisplayOrder,
                ["configuration"] = config
            };

            if (section.SectionType == "popular-hotels" || section.SectionType == "romantic-tours")
            {
                var hotels = await GetSectionHotelsData(section.Hotels, locale, cancellationToken);
                sectionData["hotels"] = hotels;
            }
            else if (section.SectionType == "popular-destinations")
            {
                var destinations = await GetSectionDestinationsData(section.Destinations, cancellationToken);
                sectionData["destinations"] = destinations;
            }

            return Ok(new { success = true, data = sectionData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching section by id");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    #region Private Helper Methods

    private async Task<List<object>> GetSectionHotelsData(
        ICollection<Domain.Entities.HomePageSectionHotel> sectionHotels,
        string locale,
        CancellationToken cancellationToken)
    {
        var result = new List<object>();

        foreach (var sh in sectionHotels.OrderBy(h => h.DisplayOrder))
        {
            if (!int.TryParse(sh.HotelId, out var hotelIdInt))
                continue;

            var hotel = await _cacheService.GetHotelByIdAsync(hotelIdInt, cancellationToken);
            if (hotel != null && hotel.Language.Equals(locale, StringComparison.OrdinalIgnoreCase))
            {
                var images = ParseJsonArray(hotel.ImageUrls);
                var features = ParseJsonArray(hotel.FeatureIds);
                var themes = ParseJsonArray(hotel.ThemeIds);

                result.Add(new
                {
                    id = hotel.HotelId,
                    name = hotel.Name,
                    description = hotel.Description,
                    stars = hotel.Category,
                    city = hotel.City,
                    country = hotel.Country,
                    countryCode = hotel.CountryCode,
                    address = hotel.Address,
                    resort = new
                    {
                        id = hotel.ResortId,
                        name = hotel.ResortName
                    },
                    location = hotel.Latitude.HasValue && hotel.Longitude.HasValue
                        ? new { latitude = hotel.Latitude.Value, longitude = hotel.Longitude.Value }
                        : null,
                    images = images?.Take(5).ToList() ?? new List<string>(),
                    featureIds = features ?? new List<string>(),
                    themeIds = themes ?? new List<string>(),
                    contact = new
                    {
                        phone = hotel.Phone,
                        email = hotel.Email,
                        website = hotel.Website
                    }
                });
            }
        }

        return result;
    }

    private async Task<List<object>> GetSectionDestinationsData(
        ICollection<Domain.Entities.HomePageSectionDestination> sectionDestinations,
        CancellationToken cancellationToken)
    {
        var result = new List<object>();

        foreach (var sd in sectionDestinations.OrderBy(d => d.DisplayOrder))
        {
            if (!int.TryParse(sd.DestinationId, out var destIdInt))
                continue;

            var destination = await _cacheService.GetDestinationByIdAsync(destIdInt, cancellationToken);
            if (destination != null)
            {
                result.Add(new
                {
                    id = destination.DestinationId,
                    code = destination.DestinationCode,
                    name = destination.Name,
                    country = destination.Country,
                    countryCode = destination.CountryCode,
                    countryId = destination.CountryId,
                    timeZone = destination.TimeZone
                });
            }
        }

        return result;
    }

    private static List<string>? ParseJsonArray(string? json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return new List<string>();
        }
    }

    #endregion
}

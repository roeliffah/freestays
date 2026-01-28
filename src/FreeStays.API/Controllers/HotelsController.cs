using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.Features.Hotels.Queries;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

public class HotelsController : BaseApiController
{
    private readonly ISunHotelsService _sunHotelsService;
    private readonly ISunHotelsCacheService _cacheService;
    private readonly ILogger<HotelsController> _logger;

    public HotelsController(ISunHotelsService sunHotelsService, ISunHotelsCacheService cacheService, ILogger<HotelsController> logger)
    {
        _sunHotelsService = sunHotelsService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Otelleri ara
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchHotels([FromQuery] SearchHotelsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Otel detaylarını getir
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotel(Guid id)
    {
        var result = await Mediator.Send(new GetHotelByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// SunHotels otel detayları (id SunHotels hotelId'dir)
    /// Frontend bu endpoint'e SunHotels ID gönderir. Tarihler opsiyoneldir.
    /// </summary>
    [HttpGet("{hotelId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelBySunHotelsId(
        int hotelId,
        [FromQuery] DateTime? checkIn,
        [FromQuery] DateTime? checkOut,
        [FromQuery] int adults = 2,
        [FromQuery] int children = 0,
        [FromQuery] string currency = "EUR",
        [FromQuery] string? destinationId = null,
        [FromQuery] string? resortId = null,
        CancellationToken cancellationToken = default)
    {
        var ci = checkIn ?? DateTime.Today.AddDays(7);
        var co = checkOut ?? DateTime.Today.AddDays(10);

        _logger.LogInformation("Getting SunHotels details for hotelId={HotelId}, ci={CheckIn}, co={CheckOut}, adults={Adults}, children={Children}", hotelId, ci, co, adults, children);

        var result = await _sunHotelsService.GetHotelDetailsAsync(hotelId, ci, co, adults, children, currency, destinationId, resortId, cancellationToken);
        if (result == null)
            return NotFound(new { message = $"SunHotels hotel {hotelId} not found or not available for selected dates" });

        // Feature ID'leri isimlerle zenginleştir (her zaman İngilizce)
        var allFeatures = await _cacheService.GetAllFeaturesAsync("en", cancellationToken);
        var allThemes = await _cacheService.GetAllThemesAsync(cancellationToken);

        var features = allFeatures
            .Where(f => result.FeatureIds.Contains(f.FeatureId))
            .Select(f => new { id = f.FeatureId, name = f.Name })
            .ToList();

        var themes = allThemes
            .Where(t => result.ThemeIds.Contains(t.ThemeId))
            .Select(t => new { id = t.ThemeId, name = t.Name })
            .ToList();

        // Boş oda listesi durumunda mesaj ekle
        string? message = null;
        if (result.Rooms == null || result.Rooms.Count == 0)
        {
            message = "Seçili tarihler için oda bulunamadı. Lütfen farklı tarihler deneyin.";
        }

        return Ok(new
        {
            message,
            result.HotelId,
            result.Name,
            result.Description,
            result.Address,
            result.City,
            result.Country,
            result.CountryCode,
            result.Category,
            result.Latitude,
            result.Longitude,
            result.GiataCode,
            result.ResortId,
            result.ResortName,
            result.Phone,
            result.Email,
            result.Website,
            result.MinPrice,
            result.Currency,
            result.ReviewScore,
            result.ReviewCount,
            result.Images,
            result.ImageUrls,
            result.Rooms,
            featureIds = result.FeatureIds,
            features, // ID + Name
            themeIds = result.ThemeIds,
            themes // ID + Name
        });
    }

    /// <summary>
    /// Öne çıkan otelleri getir
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedHotels([FromQuery] int count = 10)
    {
        var result = await Mediator.Send(new GetFeaturedHotelsQuery(count));
        return Ok(result);
    }

    /// <summary>
    /// Destinasyonları getir
    /// </summary>
    [HttpGet("destinations")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDestinations([FromQuery] GetDestinationsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir destinasyonun otellerini getir
    /// </summary>
    [HttpGet("destinations/{destinationId}/hotels")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotelsByDestination(Guid destinationId, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut, [FromQuery] int adults = 2)
    {
        var query = new SearchHotelsQuery
        {
            Destination = destinationId.ToString(),
            CheckIn = checkIn ?? DateTime.Today.AddDays(7),
            CheckOut = checkOut ?? DateTime.Today.AddDays(10),
            Guests = adults
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Oda müsaitliğini kontrol et
    /// </summary>
    [HttpGet("{id}/rooms")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomAvailability(Guid id, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut, [FromQuery] int adults = 2, [FromQuery] int children = 0)
    {
        // TODO: Implement room availability query
        return Ok(new { hotelId = id, message = "Room availability check - to be implemented" });
    }

    /// <summary>
    /// SunHotels oda müsaitliği (id SunHotels hotelId)
    /// </summary>
    [HttpGet("{hotelId:int}/rooms")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSunHotelsRoomAvailability(
        int hotelId,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] int adults = 2,
        [FromQuery] int children = 0,
        [FromQuery] string currency = "EUR",
        [FromQuery] string? destinationId = null,
        [FromQuery] string? resortId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sunHotelsService.GetHotelDetailsAsync(hotelId, checkIn, checkOut, adults, children, currency, destinationId, resortId, cancellationToken);
        if (result == null)
            return NotFound(new { message = $"SunHotels hotel {hotelId} not found for selected dates" });

        // Boş oda listesi durumunda mesaj ekle
        string? message = null;
        if (result.Rooms == null || result.Rooms.Count == 0)
        {
            message = "Seçili tarihler için oda bulunamadı. Lütfen farklı tarihler deneyin.";
        }

        // Oda bilgileri SunHotelsSearchResultV3 içinde Rooms olarak dönüyor
        return Ok(new
        {
            message,
            hotelId,
            checkIn = checkIn.ToString("yyyy-MM-dd"),
            checkOut = checkOut.ToString("yyyy-MM-dd"),
            adults,
            children,
            rooms = result.Rooms
        });
    }
}

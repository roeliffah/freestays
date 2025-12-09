using FreeStays.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

/// <summary>
/// SunHotels statik veri cache API'si
/// </summary>
[ApiController]
[Route("api/v1/sunhotels")]
[Produces("application/json")]
public class SunHotelsController : ControllerBase
{
    private readonly ISunHotelsCacheService _cacheService;
    private readonly ILogger<SunHotelsController> _logger;

    public SunHotelsController(
        ISunHotelsCacheService cacheService,
        ILogger<SunHotelsController> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Statistics

    /// <summary>
    /// Cache istatistiklerini getirir
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(SunHotelsCacheStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var stats = await _cacheService.GetCacheStatisticsAsync(cancellationToken);
        return Ok(stats);
    }

    #endregion

    #region Destinations

    /// <summary>
    /// Tüm destinasyonları getirir
    /// </summary>
    [HttpGet("destinations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDestinations(CancellationToken cancellationToken)
    {
        var destinations = await _cacheService.GetAllDestinationsAsync(cancellationToken);
        return Ok(destinations);
    }

    /// <summary>
    /// Destinasyon ID'ye göre getirir
    /// </summary>
    [HttpGet("destinations/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDestinationById(int id, CancellationToken cancellationToken)
    {
        var destination = await _cacheService.GetDestinationByIdAsync(id, cancellationToken);
        if (destination == null)
            return NotFound(new { message = $"Destination with ID {id} not found" });
        
        return Ok(destination);
    }

    /// <summary>
    /// Destinasyonlarda arama yapar
    /// </summary>
    [HttpGet("destinations/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchDestinations([FromQuery] string q, CancellationToken cancellationToken)
    {
        var destinations = await _cacheService.SearchDestinationsAsync(q, cancellationToken);
        return Ok(destinations);
    }

    #endregion

    #region Resorts

    /// <summary>
    /// Tüm resortları getirir
    /// </summary>
    [HttpGet("resorts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllResorts(CancellationToken cancellationToken)
    {
        var resorts = await _cacheService.GetAllResortsAsync(cancellationToken);
        return Ok(resorts);
    }

    /// <summary>
    /// Resort ID'ye göre getirir
    /// </summary>
    [HttpGet("resorts/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResortById(int id, CancellationToken cancellationToken)
    {
        var resort = await _cacheService.GetResortByIdAsync(id, cancellationToken);
        if (resort == null)
            return NotFound(new { message = $"Resort with ID {id} not found" });
        
        return Ok(resort);
    }

    /// <summary>
    /// Destinasyona göre resortları getirir
    /// </summary>
    [HttpGet("destinations/{destinationId:int}/resorts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResortsByDestination(int destinationId, CancellationToken cancellationToken)
    {
        var resorts = await _cacheService.GetResortsByDestinationAsync(destinationId, cancellationToken);
        return Ok(resorts);
    }

    /// <summary>
    /// Resortlarda arama yapar
    /// </summary>
    [HttpGet("resorts/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchResorts([FromQuery] string q, CancellationToken cancellationToken)
    {
        var resorts = await _cacheService.SearchResortsAsync(q, cancellationToken);
        return Ok(resorts);
    }

    #endregion

    #region Meals

    /// <summary>
    /// Tüm yemek tiplerini getirir
    /// </summary>
    [HttpGet("meals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMeals(CancellationToken cancellationToken)
    {
        var meals = await _cacheService.GetAllMealsAsync(cancellationToken);
        return Ok(meals);
    }

    /// <summary>
    /// Yemek tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("meals/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMealById(int id, CancellationToken cancellationToken)
    {
        var meal = await _cacheService.GetMealByIdAsync(id, cancellationToken);
        if (meal == null)
            return NotFound(new { message = $"Meal with ID {id} not found" });
        
        return Ok(meal);
    }

    #endregion

    #region Room Types

    /// <summary>
    /// Tüm oda tiplerini getirir
    /// </summary>
    [HttpGet("room-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoomTypes(CancellationToken cancellationToken)
    {
        var roomTypes = await _cacheService.GetAllRoomTypesAsync(cancellationToken);
        return Ok(roomTypes);
    }

    /// <summary>
    /// Oda tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("room-types/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomTypeById(int id, CancellationToken cancellationToken)
    {
        var roomType = await _cacheService.GetRoomTypeByIdAsync(id, cancellationToken);
        if (roomType == null)
            return NotFound(new { message = $"Room type with ID {id} not found" });
        
        return Ok(roomType);
    }

    #endregion

    #region Features

    /// <summary>
    /// Tüm özellikleri getirir
    /// </summary>
    [HttpGet("features")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFeatures(CancellationToken cancellationToken)
    {
        var features = await _cacheService.GetAllFeaturesAsync(cancellationToken);
        return Ok(features);
    }

    /// <summary>
    /// Özellik ID'ye göre getirir
    /// </summary>
    [HttpGet("features/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeatureById(int id, CancellationToken cancellationToken)
    {
        var feature = await _cacheService.GetFeatureByIdAsync(id, cancellationToken);
        if (feature == null)
            return NotFound(new { message = $"Feature with ID {id} not found" });
        
        return Ok(feature);
    }

    /// <summary>
    /// Özellik tipine göre filtreler
    /// </summary>
    [HttpGet("features/by-type/{featureType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturesByType(string featureType, CancellationToken cancellationToken)
    {
        var features = await _cacheService.GetFeaturesByTypeAsync(featureType, cancellationToken);
        return Ok(features);
    }

    #endregion

    #region Themes

    /// <summary>
    /// Tüm temaları getirir
    /// </summary>
    [HttpGet("themes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllThemes(CancellationToken cancellationToken)
    {
        var themes = await _cacheService.GetAllThemesAsync(cancellationToken);
        return Ok(themes);
    }

    /// <summary>
    /// Tema ID'ye göre getirir
    /// </summary>
    [HttpGet("themes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThemeById(int id, CancellationToken cancellationToken)
    {
        var theme = await _cacheService.GetThemeByIdAsync(id, cancellationToken);
        if (theme == null)
            return NotFound(new { message = $"Theme with ID {id} not found" });
        
        return Ok(theme);
    }

    #endregion

    #region Languages

    /// <summary>
    /// Tüm dilleri getirir
    /// </summary>
    [HttpGet("languages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllLanguages(CancellationToken cancellationToken)
    {
        var languages = await _cacheService.GetAllLanguagesAsync(cancellationToken);
        return Ok(languages);
    }

    /// <summary>
    /// Dil koduna göre getirir
    /// </summary>
    [HttpGet("languages/{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLanguageByCode(string code, CancellationToken cancellationToken)
    {
        var language = await _cacheService.GetLanguageByCodeAsync(code, cancellationToken);
        if (language == null)
            return NotFound(new { message = $"Language with code '{code}' not found" });
        
        return Ok(language);
    }

    #endregion

    #region Transfer Types

    /// <summary>
    /// Tüm transfer tiplerini getirir
    /// </summary>
    [HttpGet("transfer-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTransferTypes(CancellationToken cancellationToken)
    {
        var transferTypes = await _cacheService.GetAllTransferTypesAsync(cancellationToken);
        return Ok(transferTypes);
    }

    /// <summary>
    /// Transfer tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("transfer-types/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransferTypeById(int id, CancellationToken cancellationToken)
    {
        var transferType = await _cacheService.GetTransferTypeByIdAsync(id, cancellationToken);
        if (transferType == null)
            return NotFound(new { message = $"Transfer type with ID {id} not found" });
        
        return Ok(transferType);
    }

    #endregion

    #region Note Types

    /// <summary>
    /// Tüm not tiplerini getirir
    /// </summary>
    [HttpGet("note-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllNoteTypes(CancellationToken cancellationToken)
    {
        var noteTypes = await _cacheService.GetAllNoteTypesAsync(cancellationToken);
        return Ok(noteTypes);
    }

    /// <summary>
    /// Not tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("note-types/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNoteTypeById(int id, CancellationToken cancellationToken)
    {
        var noteType = await _cacheService.GetNoteTypeByIdAsync(id, cancellationToken);
        if (noteType == null)
            return NotFound(new { message = $"Note type with ID {id} not found" });
        
        return Ok(noteType);
    }

    #endregion

    #region Hotels

    /// <summary>
    /// Otelleri sayfalanmış olarak getirir
    /// </summary>
    [HttpGet("hotels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotelsPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? destinationId = null,
        [FromQuery] int? resortId = null,
        [FromQuery] int? minStars = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (hotels, totalCount) = await _cacheService.GetHotelsPaginatedAsync(
            page, pageSize, destinationId, resortId, minStars, cancellationToken);

        return Ok(new
        {
            data = hotels,
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Otel ID'ye göre getirir
    /// </summary>
    [HttpGet("hotels/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelById(int id, CancellationToken cancellationToken)
    {
        var hotel = await _cacheService.GetHotelByIdAsync(id, cancellationToken);
        if (hotel == null)
            return NotFound(new { message = $"Hotel with ID {id} not found" });
        
        return Ok(hotel);
    }

    /// <summary>
    /// Otellerde arama yapar
    /// </summary>
    [HttpGet("hotels/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchHotels([FromQuery] string q, CancellationToken cancellationToken)
    {
        var hotels = await _cacheService.SearchHotelsAsync(q, cancellationToken);
        return Ok(hotels);
    }

    /// <summary>
    /// Destinasyona göre otelleri getirir
    /// </summary>
    [HttpGet("destinations/{destinationId:int}/hotels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotelsByDestination(int destinationId, CancellationToken cancellationToken)
    {
        var hotels = await _cacheService.GetHotelsByDestinationAsync(destinationId, cancellationToken);
        return Ok(hotels);
    }

    /// <summary>
    /// Resorta göre otelleri getirir
    /// </summary>
    [HttpGet("resorts/{resortId:int}/hotels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotelsByResort(int resortId, CancellationToken cancellationToken)
    {
        var hotels = await _cacheService.GetHotelsByResortAsync(resortId, cancellationToken);
        return Ok(hotels);
    }

    #endregion

    #region Rooms

    /// <summary>
    /// Otele göre odaları getirir
    /// </summary>
    [HttpGet("hotels/{hotelId:int}/rooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoomsByHotel(int hotelId, CancellationToken cancellationToken)
    {
        var rooms = await _cacheService.GetRoomsByHotelAsync(hotelId, cancellationToken);
        return Ok(rooms);
    }

    /// <summary>
    /// Oda ID'ye göre getirir
    /// </summary>
    [HttpGet("rooms/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomById(int id, CancellationToken cancellationToken)
    {
        var room = await _cacheService.GetRoomByIdAsync(id, cancellationToken);
        if (room == null)
            return NotFound(new { message = $"Room with ID {id} not found" });
        
        return Ok(room);
    }

    #endregion
}

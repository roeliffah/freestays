using FreeStays.Application.Common.Interfaces;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

/// <summary>
/// SunHotels statik veri cache ve dinamik rezervasyon API'si
/// </summary>
[ApiController]
[Route("api/v1/sunhotels")]
[Produces("application/json")]
public class SunHotelsController : ControllerBase
{
    private readonly ISunHotelsCacheService _cacheService;
    private readonly ISunHotelsService _sunHotelsService;
    private readonly ILogger<SunHotelsController> _logger;

    public SunHotelsController(
        ISunHotelsCacheService cacheService,
        ISunHotelsService sunHotelsService,
        ILogger<SunHotelsController> logger)
    {
        _cacheService = cacheService;
        _sunHotelsService = sunHotelsService;
        _logger = logger;
    }

    #region Statistics

    /// <summary>
    /// Cache istatistiklerini getirir
    /// </summary>
    [HttpGet("statistics")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    #region Non-Static API - Dynamic Pricing & Booking

    /// <summary>
    /// Otel arama - Canlı fiyatlar ve müsaitlik (V3)
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(List<SunHotelsSearchResultV3>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchHotels([FromBody] SunHotelsSearchRequestV3 request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Searching hotels with SunHotels API - Destination: {Destination}, CheckIn: {CheckIn}, CheckOut: {CheckOut}",
                request.Destination, request.CheckIn, request.CheckOut);

            var results = await _sunHotelsService.SearchHotelsV3Async(request, cancellationToken);

            _logger.LogInformation("Found {Count} hotels", results.Count);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hotels");
            return StatusCode(500, new { message = "Error searching hotels", error = ex.Message });
        }
    }

    /// <summary>
    /// Otel detayları - Canlı fiyat ve müsaitlik
    /// </summary>
    [HttpGet("hotels/{hotelId:int}/details")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SunHotelsSearchResultV3), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelDetails(
        int hotelId,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] int adults = 2,
        [FromQuery] int children = 0,
        [FromQuery] string currency = "EUR",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sunHotelsService.GetHotelDetailsAsync(hotelId, checkIn, checkOut, adults, children, currency, cancellationToken);

            if (result == null)
                return NotFound(new { message = $"Hotel {hotelId} not found or not available for the selected dates" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel details for hotel {HotelId}", hotelId);
            return StatusCode(500, new { message = "Error getting hotel details", error = ex.Message });
        }
    }

    /// <summary>
    /// Ön rezervasyon - Fiyat kilitleme (V3)
    /// </summary>
    [HttpPost("prebook")]
    [ProducesResponseType(typeof(SunHotelsPreBookResultV3), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreBook([FromBody] SunHotelsPreBookRequestV3 request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating prebook for hotel {HotelId}", request.HotelId);

            var result = await _sunHotelsService.PreBookV3Async(request, cancellationToken);

            _logger.LogInformation("Prebook created with code: {PreBookCode}", result.PreBookCode);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prebook");
            return StatusCode(500, new { message = "Error creating prebook", error = ex.Message });
        }
    }

    /// <summary>
    /// Rezervasyon yapma (V3)
    /// </summary>
    [HttpPost("book")]
    [ProducesResponseType(typeof(SunHotelsBookResultV3), StatusCodes.Status200OK)]
    public async Task<IActionResult> Book([FromBody] SunHotelsBookRequestV3 request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating booking with prebook code: {PreBookCode}", request.PreBookCode);

            var result = await _sunHotelsService.BookV3Async(request, cancellationToken);

            _logger.LogInformation("Booking created with number: {BookingNumber}", result.BookingNumber);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, new { message = "Error creating booking", error = ex.Message });
        }
    }

    /// <summary>
    /// Rezervasyon sorgulama
    /// </summary>
    [HttpPost("booking/query")]
    [ProducesResponseType(typeof(List<SunHotelsBookingInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookingInformation([FromBody] SunHotelsGetBookingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var results = await _sunHotelsService.GetBookingInformationAsync(request, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking information");
            return StatusCode(500, new { message = "Error getting booking information", error = ex.Message });
        }
    }

    /// <summary>
    /// Rezervasyon iptal etme
    /// </summary>
    [HttpPost("booking/{bookingId}/cancel")]
    [ProducesResponseType(typeof(SunHotelsCancelResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelBooking(
        string bookingId,
        [FromQuery] string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling booking: {BookingId}", bookingId);

            var result = await _sunHotelsService.CancelBookingAsync(bookingId, language, cancellationToken);

            _logger.LogInformation("Booking {BookingId} cancelled successfully", bookingId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
            return StatusCode(500, new { message = "Error cancelling booking", error = ex.Message });
        }
    }

    /// <summary>
    /// Rezervasyon değişikliği fiyat sorgulama
    /// </summary>
    [HttpPost("booking/amendment/price")]
    [ProducesResponseType(typeof(SunHotelsAmendmentPriceResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAmendmentPrice([FromBody] SunHotelsAmendmentPriceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sunHotelsService.GetAmendmentPriceAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting amendment price");
            return StatusCode(500, new { message = "Error getting amendment price", error = ex.Message });
        }
    }

    /// <summary>
    /// Rezervasyon değişikliği yapma
    /// </summary>
    [HttpPost("booking/amendment")]
    [ProducesResponseType(typeof(SunHotelsAmendmentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> AmendBooking([FromBody] SunHotelsAmendmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Amending booking: {BookingId}", request.BookingId);

            var result = await _sunHotelsService.AmendBookingAsync(request, cancellationToken);

            _logger.LogInformation("Booking {BookingId} amended successfully", request.BookingId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error amending booking");
            return StatusCode(500, new { message = "Error amending booking", error = ex.Message });
        }
    }

    /// <summary>
    /// Özel istek güncelleme
    /// </summary>
    [HttpPut("booking/{bookingId}/special-request")]
    [ProducesResponseType(typeof(SunHotelsSpecialRequestResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSpecialRequest(
        string bookingId,
        [FromBody] UpdateSpecialRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sunHotelsService.UpdateSpecialRequestAsync(bookingId, request.Text, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating special request");
            return StatusCode(500, new { message = "Error updating special request", error = ex.Message });
        }
    }

    /// <summary>
    /// Özel istek sorgulama
    /// </summary>
    [HttpGet("booking/{bookingId}/special-request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SunHotelsSpecialRequestResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialRequest(string bookingId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sunHotelsService.GetSpecialRequestAsync(bookingId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting special request");
            return StatusCode(500, new { message = "Error getting special request", error = ex.Message });
        }
    }

    /// <summary>
    /// Transfer arama (V2)
    /// </summary>
    [HttpPost("transfers/search")]
    [ProducesResponseType(typeof(List<SunHotelsTransferSearchResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTransfers([FromBody] SunHotelsTransferSearchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var results = await _sunHotelsService.SearchTransfersAsync(request, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transfers");
            return StatusCode(500, new { message = "Error searching transfers", error = ex.Message });
        }
    }

    /// <summary>
    /// Transfer ekleme (V2)
    /// </summary>
    [HttpPost("transfers/add")]
    [ProducesResponseType(typeof(SunHotelsAddTransferResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddTransfer([FromBody] SunHotelsAddTransferRequestV2 request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Adding transfer to booking");

            var result = await _sunHotelsService.AddTransferAsync(request, cancellationToken);

            _logger.LogInformation("Transfer added with booking ID: {TransferBookingId}", result.TransferBookingId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transfer");
            return StatusCode(500, new { message = "Error adding transfer", error = ex.Message });
        }
    }

    /// <summary>
    /// Transfer iptal etme
    /// </summary>
    [HttpPost("transfers/{transferBookingId}/cancel")]
    [ProducesResponseType(typeof(SunHotelsCancelTransferResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelTransfer(
        string transferBookingId,
        [FromQuery] string email,
        [FromQuery] string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling transfer: {TransferBookingId}", transferBookingId);

            var result = await _sunHotelsService.CancelTransferAsync(transferBookingId, email, language, cancellationToken);

            _logger.LogInformation("Transfer {TransferBookingId} cancelled successfully", transferBookingId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer");
            return StatusCode(500, new { message = "Error cancelling transfer", error = ex.Message });
        }
    }

    /// <summary>
    /// Transfer rezervasyon sorgulama
    /// </summary>
    [HttpGet("transfers/bookings")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<SunHotelsBookingInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransferBookings(
        [FromQuery] string? bookingId = null,
        [FromQuery] DateTime? createdDateFrom = null,
        [FromQuery] DateTime? createdDateTo = null,
        [FromQuery] DateTime? arrivalDateFrom = null,
        [FromQuery] DateTime? arrivalDateTo = null,
        [FromQuery] string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _sunHotelsService.GetTransferBookingInformationAsync(
                bookingId, createdDateFrom, createdDateTo, arrivalDateFrom, arrivalDateTo, language, cancellationToken);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer bookings");
            return StatusCode(500, new { message = "Error getting transfer bookings", error = ex.Message });
        }
    }

    #endregion
}

public record UpdateSpecialRequestDto(string Text);

using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.SunHotels;
using FreeStays.Application.Features.Settings.Queries;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using MediatR;
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
    private readonly ITranslationRepository _translationRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<SunHotelsController> _logger;

    public SunHotelsController(
        ISunHotelsCacheService cacheService,
        ISunHotelsService sunHotelsService,
        ITranslationRepository translationRepository,
        IMediator mediator,
        ILogger<SunHotelsController> logger)
    {
        _cacheService = cacheService;
        _mediator = mediator;
        _sunHotelsService = sunHotelsService;
        _translationRepository = translationRepository;
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

    /// <summary>
    /// Tüm ülkeleri getirir (destinasyonlardan gruplandırılmış)
    /// </summary>
    [HttpGet("countries")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCountries(CancellationToken cancellationToken)
    {
        var destinations = await _cacheService.GetAllDestinationsAsync(cancellationToken);

        var countries = destinations
            .GroupBy(d => new { d.Country, d.CountryCode, d.CountryId })
            .Select(g => new
            {
                name = g.Key.Country,
                code = g.Key.CountryCode,
                countryId = g.Key.CountryId,
                destinationCount = g.Count()
            })
            .OrderBy(c => c.name)
            .ToList();

        return Ok(countries);
    }

    #endregion

    #region Resorts

    /// <summary>
    /// Tüm resortları getirir
    /// </summary>
    [HttpGet("resorts")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllResorts(CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var resorts = await _cacheService.GetAllResortsAsync(language, cancellationToken);
        return Ok(resorts);
    }

    /// <summary>
    /// Resort ID'ye göre getirir
    /// </summary>
    [HttpGet("resorts/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResortById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var resort = await _cacheService.GetResortByIdAsync(id, language, cancellationToken);
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
    public async Task<IActionResult> GetResortsByDestination(int destinationId, CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var resorts = await _cacheService.GetResortsByDestinationAsync(destinationId, language, cancellationToken);
        return Ok(resorts);
    }

    /// <summary>
    /// Resortlarda arama yapar
    /// </summary>
    [HttpGet("resorts/search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchResorts([FromQuery] string q, CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var resorts = await _cacheService.SearchResortsAsync(q, language, cancellationToken);
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
    public async Task<IActionResult> GetAllMeals(CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var meals = await _cacheService.GetAllMealsAsync(language, cancellationToken);
        return Ok(meals);
    }

    /// <summary>
    /// Yemek tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("meals/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMealById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var meal = await _cacheService.GetMealByIdAsync(id, language, cancellationToken);
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
    public async Task<IActionResult> GetAllRoomTypes(CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var roomTypes = await _cacheService.GetAllRoomTypesAsync(language, cancellationToken);
        return Ok(roomTypes);
    }

    /// <summary>
    /// Oda tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("room-types/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomTypeById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var roomType = await _cacheService.GetRoomTypeByIdAsync(id, language, cancellationToken);
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
    public async Task<IActionResult> GetAllFeatures(CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var features = await _cacheService.GetAllFeaturesAsync(language, cancellationToken);
        return Ok(features);
    }

    /// <summary>
    /// Özellik ID'ye göre getirir
    /// </summary>
    [HttpGet("features/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeatureById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var feature = await _cacheService.GetFeatureByIdAsync(id, language, cancellationToken);
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
    public async Task<IActionResult> GetFeaturesByType(string featureType, CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var features = await _cacheService.GetFeaturesByTypeAsync(featureType, language, cancellationToken);
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
    public async Task<IActionResult> GetAllThemes(CancellationToken cancellationToken, [FromQuery] string language = "en")
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
    public async Task<IActionResult> GetAllLanguages(CancellationToken cancellationToken, [FromQuery] string language = "en")
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
    public async Task<IActionResult> GetAllTransferTypes(CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var transferTypes = await _cacheService.GetAllTransferTypesAsync(language, cancellationToken);
        return Ok(transferTypes);
    }

    /// <summary>
    /// Transfer tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("transfer-types/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransferTypeById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var transferType = await _cacheService.GetTransferTypeByIdAsync(id, language, cancellationToken);
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
    public async Task<IActionResult> GetAllNoteTypes(CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var noteTypes = await _cacheService.GetAllNoteTypesAsync(language, cancellationToken);
        return Ok(noteTypes);
    }

    /// <summary>
    /// Not tipi ID'ye göre getirir
    /// </summary>
    [HttpGet("note-types/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNoteTypeById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var noteType = await _cacheService.GetNoteTypeByIdAsync(id, language, cancellationToken);
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
        CancellationToken cancellationToken = default,
        [FromQuery] string language = "en")
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (hotels, totalCount) = await _cacheService.GetHotelsPaginatedAsync(
            page, pageSize, language, destinationId, resortId, minStars, cancellationToken);

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
    public async Task<IActionResult> GetHotelById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var hotel = await _cacheService.GetHotelByIdAsync(id, language, cancellationToken);
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
    public async Task<IActionResult> SearchHotels([FromQuery] string q, CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var hotels = await _cacheService.SearchHotelsAsync(q, language, cancellationToken);
        return Ok(hotels);
    }

    /// <summary>
    /// Destinasyona göre otelleri getirir
    /// </summary>
    [HttpGet("destinations/{destinationId:int}/hotels")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotelsByDestination(int destinationId, CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var hotels = await _cacheService.GetHotelsByDestinationAsync(destinationId, language, cancellationToken);
        return Ok(hotels);
    }

    /// <summary>
    /// Resorta göre otelleri getirir
    /// </summary>
    [HttpGet("resorts/{resortId:int}/hotels")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotelsByResort(int resortId, CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var hotels = await _cacheService.GetHotelsByResortAsync(resortId, language, cancellationToken);
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
    public async Task<IActionResult> GetRoomsByHotel(int hotelId, CancellationToken cancellationToken, [FromQuery] string language = "en")
    {
        var rooms = await _cacheService.GetRoomsByHotelAsync(hotelId, language, cancellationToken);
        return Ok(rooms);
    }

    /// <summary>
    /// Oda ID'ye göre getirir
    /// </summary>
    [HttpGet("rooms/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomById(int id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        var language = ParseLanguage(acceptLanguage);
        var room = await _cacheService.GetRoomByIdAsync(id, language, cancellationToken);
        if (room == null)
            return NotFound(new { message = $"Room with ID {id} not found" });

        return Ok(room);
    }

    #endregion

    #region Unified Search Endpoint (Next.js)

    /// <summary>
    /// Unified Hotel Search - Next.js için
    /// Tarih varsa: Dinamik arama (non-static API ile canlı fiyatlar)
    /// Tarih yoksa: Statik arama (cache tablolardan tema, konum, ülke, özellik filtreleme)
    /// </summary>
    [HttpPost("search/unified")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HotelSearchResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnifiedHotelSearch(
        [FromBody] HotelSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Tarih kriteri varsa: Dinamik arama (non-static API)
            if (request.HasDateCriteria)
            {
                _logger.LogInformation(
                    "Dynamic hotel search - Destination: {Destinations}, CheckIn: {CheckIn}, CheckOut: {CheckOut}, Language: {Language}",
                    request.DestinationIds != null ? string.Join(",", request.DestinationIds) : "All",
                    request.CheckInDate,
                    request.CheckOutDate,
                    request.Language);

                // SunHotels API için request oluştur
                var sunHotelsRequest = new SunHotelsSearchRequestV3
                {
                    CheckIn = request.CheckInDate!.Value,
                    CheckOut = request.CheckOutDate!.Value,
                    Language = request.Language,
                    Currency = request.Currency ?? "EUR",
                    Adults = request.Adults ?? 2,
                    Children = request.Children ?? 0,
                    NumberOfRooms = request.NumberOfRooms ?? 1,
                    ChildrenAges = request.ChildrenAges ?? string.Empty,
                    Infant = request.Infant ?? 0,
                    CustomerCountry = request.CustomerCountry,
                    B2C = false,

                    // Filtreler
                    MealIds = request.MealIds != null ? string.Join(",", request.MealIds) : null,
                    FeatureIds = request.FeatureIds != null ? string.Join(",", request.FeatureIds) : null,
                    ThemeIds = request.ThemeIds != null ? string.Join(",", request.ThemeIds) : null,
                    MinStarRating = request.MinStars,
                    MaxStarRating = request.MaxStars,
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,

                    // Konum bazlı arama
                    ReferenceLatitude = request.ReferencePointLatitude,
                    ReferenceLongitude = request.ReferencePointLongitude,
                    MaxDistanceKm = request.MaxDistanceFromReferencePoint,

                    // Sıralama
                    SortBy = request.SortBy ?? "price",
                    SortOrder = request.SortOrder ?? "asc",

                    // Özel filtreler
                    AccommodationTypes = request.AccommodationTypes,
                    ExactDestinationMatch = request.ExactDestinationMatch,
                    BlockSuperdeal = request.BlockSuperdeal,
                    ExcludeSharedRooms = request.ExcludeSharedRooms ?? false,
                    ExcludeSharedFacilities = request.ExcludeSharedFacilities ?? false,
                    PrioritizedHotelIds = request.PrioritizedHotelIds,
                    TotalRoomsInBatch = request.TotalRoomsInBatch,
                    PaymentMethodId = request.PaymentMethodId,

                    // Gösterim ayarları
                    ShowCoordinates = request.ShowCoordinates ?? true,
                    ShowReviews = request.ShowReviews ?? true,
                    ShowRoomTypeName = request.ShowRoomTypeName ?? true
                };

                // SunHotels API sadece destination, destinationID, hotelIDs veya resortIDs'den BİRİNİ kabul eder
                // Öncelik sırası: HotelIds > ResortIds > DestinationId
                if (request.HotelIds != null && request.HotelIds.Any())
                {
                    sunHotelsRequest.HotelIds = string.Join(",", request.HotelIds);
                }
                else if (request.ResortIds != null && request.ResortIds.Any())
                {
                    sunHotelsRequest.ResortIds = string.Join(",", request.ResortIds);
                }
                else
                {
                    sunHotelsRequest.DestinationId = request.DestinationIds?.FirstOrDefault() ?? "10025";
                }

                // Get pricing settings
                var allSettings = await _mediator.Send(new GetSiteSettingsQuery("site"), cancellationToken);
                var settingsDict = allSettings.ToDictionary(s => s.Key, s => s.Value);

                var profitMarginStr = settingsDict.GetValueOrDefault("profitMargin", "0");
                var defaultVatRateStr = settingsDict.GetValueOrDefault("defaultVatRate", "0");
                var extraFeeStr = settingsDict.GetValueOrDefault("extraFee", "0");

                var profitMargin = decimal.TryParse(profitMarginStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pm) ? pm / 100 : 0m;
                var defaultVatRate = decimal.TryParse(defaultVatRateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vat) ? vat / 100 : 0m;
                var extraFee = decimal.TryParse(extraFeeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ef) ? ef : 0m;

                var apiResults = await _sunHotelsService.SearchHotelsV3Async(sunHotelsRequest, cancellationToken);

                // API sonuçlarını DTO'ya dönüştür ve fiyat hesapla
                var response = new HotelSearchResponse
                {
                    Hotels = apiResults.Select(r =>
                    {
                        // Calculate final price with profit margin and VAT
                        var basePrice = r.MinPrice == 0 ? 0m : r.MinPrice;
                        var profitAmount = basePrice * profitMargin;
                        var taxAmount = profitAmount * defaultVatRate;
                        var finalPrice = basePrice + profitAmount + taxAmount + extraFee;

                        return new HotelSearchResultDto
                        {
                            HotelId = r.HotelId,
                            Name = r.Name,
                            Description = r.Description,
                            Address = r.Address,
                            City = r.City,
                            Country = r.Country,
                            CountryCode = r.CountryCode,
                            Category = r.Category,
                            Latitude = r.Latitude,
                            Longitude = r.Longitude,
                            ResortId = r.ResortId,
                            ResortName = r.ResortName,
                            MinPrice = finalPrice,
                            Currency = r.Currency,
                            ReviewScore = r.ReviewScore.HasValue ? (decimal?)r.ReviewScore.Value : null,
                            ReviewCount = r.ReviewCount,
                            CheckInDate = request.CheckInDate?.ToString("yyyy-MM-dd"),
                            CheckOutDate = request.CheckOutDate?.ToString("yyyy-MM-dd"),
                            ThemeIds = r.ThemeIds,
                            FeatureIds = r.FeatureIds,
                            ImageUrls = FixImageUrls(r.ImageUrls),
                            Rooms = r.Rooms?.Select(room =>
                            {
                                // Calculate room price with profit margin and VAT
                                var roomBasePrice = room.Price == 0 ? 0m : room.Price;
                                var roomProfitAmount = roomBasePrice * profitMargin;
                                var roomTaxAmount = roomProfitAmount * defaultVatRate;
                                var roomFinalPrice = roomBasePrice + roomProfitAmount + roomTaxAmount + extraFee;

                                return new HotelRoomDto
                                {
                                    RoomId = room.RoomId.ToString(),
                                    Name = room.Name,
                                    RoomTypeName = room.RoomTypeName,
                                    MealName = room.MealName,
                                    Price = roomFinalPrice,
                                    AvailableRooms = room.AvailableRooms,
                                    IsRefundable = room.IsRefundable,
                                    IsSuperDeal = room.IsSuperDeal
                                };
                            }).ToList()
                        };
                    }).ToList(),
                    TotalCount = apiResults.Count,
                    TotalPages = (int)Math.Ceiling((double)apiResults.Count / request.PageSize),
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    SearchType = "dynamic",
                    HasPricing = true
                };

                // Fiyat gösterimlerini ayarla
                var fromText = await _translationRepository.GetTranslationAsync("hotel_search.price.from", request.Language);
                var perNightText = await _translationRepository.GetTranslationAsync("hotel_search.price.per_night", request.Language);

                foreach (var hotel in response.Hotels)
                {
                    if (hotel.MinPrice.HasValue)
                    {
                        hotel.PriceDisplay = $"{fromText} ${hotel.MinPrice:F2}{perNightText}";
                    }
                }

                _logger.LogInformation("Dynamic search completed - Found {Count} hotels", response.TotalCount);
                return Ok(response);
            }
            else
            {
                // Tarih yoksa: Statik arama (cache tablolardan)
                _logger.LogInformation(
                    "Static hotel search - Themes: {Themes}, Features: {Features}, Country: {Country}, Language: {Language}",
                    request.ThemeIds != null ? string.Join(",", request.ThemeIds) : "All",
                    request.FeatureIds != null ? string.Join(",", request.FeatureIds) : "All",
                    request.CountryCodes != null ? string.Join(",", request.CountryCodes) : "All",
                    request.Language);

                var response = await _cacheService.SearchHotelsAdvancedAsync(request, cancellationToken);

                // Fiyat bilgisi yok - tarih seçim mesajı ekle
                var priceMessage = await _translationRepository.GetTranslationAsync(
                    "hotel_search.price.select_dates",
                    request.Language);

                response.HasPricing = false;
                response.PriceMessage = priceMessage;

                // Her otel için fiyat gösterim mesajı
                foreach (var hotel in response.Hotels)
                {
                    hotel.PriceDisplay = priceMessage;
                }

                _logger.LogInformation("Static search completed - Found {Count} hotels", response.TotalCount);
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in unified hotel search");
            return StatusCode(500, new { message = "Error searching hotels", error = ex.Message });
        }
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
    /// ÖNEMLİ: destinationId veya resortId parametrelerinden en az biri önerilir (daha iyi sonuçlar için)
    /// </summary>
    [HttpGet("hotels/{hotelId:int}/details")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotelDetails(
        int hotelId,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] int adults = 2,
        [FromQuery] int children = 0,
        [FromQuery] string currency = "EUR",
        [FromQuery] string? destinationId = null,
        [FromQuery] string? resortId = null,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var language = ParseLanguage(acceptLanguage);

            // Get pricing settings
            var allSettings = await _mediator.Send(new GetSiteSettingsQuery("site"), cancellationToken);
            var settingsDict = allSettings.ToDictionary(s => s.Key, s => s.Value);

            var profitMarginStr = settingsDict.GetValueOrDefault("profitMargin", "0");
            var defaultVatRateStr = settingsDict.GetValueOrDefault("defaultVatRate", "0");
            var extraFeeStr = settingsDict.GetValueOrDefault("extraFee", "0");

            var profitMargin = decimal.TryParse(profitMarginStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pm) ? pm / 100 : 0m;
            var defaultVatRate = decimal.TryParse(defaultVatRateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vat) ? vat / 100 : 0m;
            var extraFee = decimal.TryParse(extraFeeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ef) ? ef : 0m;

            // Get live hotel data with destination context (Python best practice)
            var result = await _sunHotelsService.GetHotelDetailsAsync(hotelId, checkIn, checkOut, adults, children, currency, destinationId, resortId, cancellationToken);

            if (result == null)
                return NotFound(new { message = $"Hotel {hotelId} not found or not available for the selected dates" });

            // Get additional details from cache (features, themes)
            var allFeatures = await _cacheService.GetAllFeaturesAsync(language, cancellationToken);
            var allThemes = await _cacheService.GetAllThemesAsync(cancellationToken);

            // Calculate final prices
            var basePrice = result.MinPrice == 0 ? 0m : result.MinPrice;
            var profitAmount = basePrice * profitMargin;
            var taxAmount = profitAmount * defaultVatRate;
            var finalMinPrice = basePrice + profitAmount + taxAmount + extraFee;

            // Fix image URLs
            var fixedImages = FixImageUrls(result.ImageUrls);

            // Calculate room prices and build room details
            var roomsWithDetails = result.Rooms?.Select(room =>
            {
                var roomBasePrice = room.Price == 0 ? 0m : room.Price;
                var roomProfitAmount = roomBasePrice * profitMargin;
                var roomTaxAmount = roomProfitAmount * defaultVatRate;
                var roomFinalPrice = roomBasePrice + roomProfitAmount + roomTaxAmount + extraFee;

                // Calculate nights
                var nights = (checkOut - checkIn).Days;
                var pricePerNight = nights > 0 ? roomFinalPrice / nights : roomFinalPrice;

                return new
                {
                    roomId = room.RoomId,
                    roomTypeId = room.RoomTypeId,
                    roomTypeName = room.RoomTypeName,
                    name = room.Name,
                    description = room.Description,
                    mealId = room.MealId,
                    mealName = room.MealName,
                    price = new
                    {
                        total = roomFinalPrice,
                        perNight = pricePerNight,
                        currency = room.Currency,
                        nights = nights
                    },
                    pricing = new
                    {
                        originalPrice = room.OriginalPrice,
                        currentPrice = roomFinalPrice,
                        discount = room.OriginalPrice.HasValue && room.OriginalPrice.Value > roomFinalPrice
                            ? room.OriginalPrice.Value - roomFinalPrice
                            : (decimal?)null,
                        discountPercentage = room.OriginalPrice.HasValue && room.OriginalPrice.Value > 0
                            ? Math.Round(((room.OriginalPrice.Value - roomFinalPrice) / room.OriginalPrice.Value) * 100, 2)
                            : (decimal?)null
                    },
                    availability = new
                    {
                        availableRooms = room.AvailableRooms,
                        isAvailable = room.AvailableRooms > 0
                    },
                    policies = new
                    {
                        isRefundable = room.IsRefundable,
                        isSuperDeal = room.IsSuperDeal,
                        cancellationPolicies = room.CancellationPolicies?.Select(p => new
                        {
                            fromDate = p.FromDate.ToString("yyyy-MM-dd"),
                            percentage = p.Percentage,
                            fixedAmount = p.FixedAmount,
                            nightsCharged = p.NightsCharged
                        }).ToList(),
                        earliestFreeCancellation = room.EarliestNonFreeCancellationDate?.ToString("yyyy-MM-dd")
                    },
                    paymentMethods = room.PaymentMethodIds
                };
            }).ToList();

            // Get feature details
            var hotelFeatures = allFeatures
                .Where(f => result.FeatureIds.Contains(f.FeatureId) && f.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .Select(f => new
                {
                    id = f.FeatureId,
                    name = f.Name
                })
                .ToList();

            // Get theme details
            var hotelThemes = allThemes
                .Where(t => result.ThemeIds.Contains(t.ThemeId))
                .Select(t => new
                {
                    id = t.ThemeId,
                    name = t.Name,
                    englishName = t.EnglishName
                })
                .ToList();

            // Build enhanced response
            var response = new
            {
                hotel = new
                {
                    id = result.HotelId,
                    name = result.Name,
                    description = result.Description,
                    category = result.Category,
                    stars = result.Category,
                    contact = new
                    {
                        address = result.Address,
                        city = result.City,
                        country = result.Country,
                        countryCode = result.CountryCode,
                        phone = result.Phone,
                        email = result.Email,
                        website = result.Website
                    },
                    location = new
                    {
                        latitude = result.Latitude,
                        longitude = result.Longitude,
                        resort = new
                        {
                            id = result.ResortId,
                            name = result.ResortName
                        },
                        giataCode = result.GiataCode
                    },
                    images = fixedImages,
                    pricing = new
                    {
                        minPrice = finalMinPrice,
                        currency = result.Currency,
                        checkIn = checkIn.ToString("yyyy-MM-dd"),
                        checkOut = checkOut.ToString("yyyy-MM-dd"),
                        nights = (checkOut - checkIn).Days,
                        adults,
                        children
                    },
                    reviews = result.ReviewScore.HasValue || result.ReviewCount.HasValue
                        ? new
                        {
                            score = result.ReviewScore,
                            count = result.ReviewCount,
                            rating = result.ReviewScore.HasValue ? GetRatingText(result.ReviewScore.Value) : null
                        }
                        : null,
                    features = hotelFeatures,
                    themes = hotelThemes,
                    totalRooms = roomsWithDetails?.Count ?? 0
                },
                rooms = roomsWithDetails,
                availability = new
                {
                    hasAvailableRooms = roomsWithDetails?.Any(r => r.availability.isAvailable) ?? false,
                    totalAvailableRooms = roomsWithDetails?.Sum(r => r.availability.availableRooms) ?? 0
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel details for hotel {HotelId}", hotelId);
            return StatusCode(500, new { message = "Error getting hotel details", error = ex.Message });
        }
    }

    private static string GetRatingText(double score)
    {
        return score switch
        {
            >= 9.0 => "Excellent",
            >= 8.0 => "Very Good",
            >= 7.0 => "Good",
            >= 6.0 => "Pleasant",
            _ => "Fair"
        };
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

    /// <summary>
    /// Accept-Language header'dan dil kodunu parse eder
    /// </summary>
    private static string ParseLanguage(string? acceptLanguage)
    {
        if (string.IsNullOrEmpty(acceptLanguage))
            return "en";

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
        return "en";
    }

    /// <summary>
    /// SunHotels resim URL'lerini düzgün formata dönüştürür
    /// http://xml.sunhotels.net/15/GetImage.aspx?id=78713614 
    /// → https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id=78713614&amp;full=1
    /// </summary>
    private static List<string> FixImageUrls(List<string>? imageUrls)
    {
        if (imageUrls == null || !imageUrls.Any())
            return new List<string>();

        return imageUrls.Select(url =>
        {
            if (string.IsNullOrEmpty(url))
                return url;

            // Eski format: http://xml.sunhotels.net/15/GetImage.aspx?id=78713614
            if (url.Contains("xml.sunhotels.net") && url.Contains("GetImage.aspx"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"id=(\d+)");
                if (match.Success)
                {
                    var imageId = match.Groups[1].Value;
                    return $"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={imageId}&full=1";
                }
            }

            return url;
        }).ToList();
    }

    #endregion
}

public record UpdateSpecialRequestDto(string Text);

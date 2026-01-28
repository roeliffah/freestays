using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Entities.Cache;
using FreeStays.Domain.Exceptions;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Caching;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreeStays.Infrastructure.ExternalServices.SunHotels;

public class SunHotelsService : ISunHotelsService
{
    private readonly HttpClient _httpClient;
    private SunHotelsConfig _config;
    private readonly ILogger<SunHotelsService> _logger;
    private readonly IExternalServiceConfigRepository _serviceConfigRepository;
    private readonly ISunHotelsCacheService _cacheService;
    private readonly SunHotelsRedisCacheService _redisCache;
    private volatile bool _configLoaded = false;
    private readonly SemaphoreSlim _configLoadLock = new(1, 1);

    private string _nonStaticApiUrl = "https://xml.sunhotels.net/15/PostGet/NonStaticXMLAPI.asmx";
    private string _staticApiUrl = "https://xml.sunhotels.net/15/PostGet/StaticXMLAPI.asmx";
    private const string NonStaticLanguage = "en";
    private const string NonStaticCurrency = "EUR";

    public SunHotelsService(
        HttpClient httpClient,
        IOptions<SunHotelsConfig> config,
        ILogger<SunHotelsService> logger,
        IExternalServiceConfigRepository serviceConfigRepository,
        ISunHotelsCacheService cacheService,
        SunHotelsRedisCacheService redisCache)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _serviceConfigRepository = serviceConfigRepository;
        _cacheService = cacheService;
        _redisCache = redisCache;

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
    }

    private async Task EnsureConfigLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_configLoaded) return;

        // Thread-safe config yükleme (double-checked locking)
        await _configLoadLock.WaitAsync(cancellationToken);
        try
        {
            if (_configLoaded) return; // İkinci kontrol (lock aldıktan sonra)

            var dbConfig = await _serviceConfigRepository.GetByServiceNameAsync("SunHotels", cancellationToken);

            if (dbConfig != null && dbConfig.IsActive)
            {
                _config.Username = dbConfig.Username ?? string.Empty;
                _config.Password = dbConfig.Password ?? string.Empty;
                _config.BaseUrl = dbConfig.BaseUrl;
                _config.AffiliateCode = dbConfig.AffiliateCode;

                // Settings JSON'dan static ve non-static URL'leri oku
                if (!string.IsNullOrEmpty(dbConfig.Settings))
                {
                    try
                    {
                        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(dbConfig.Settings);
                        if (settings != null)
                        {
                            if (settings.TryGetValue("staticApiUrl", out var staticUrl))
                                _staticApiUrl = staticUrl.ToString() ?? _staticApiUrl;

                            if (settings.TryGetValue("nonStaticApiUrl", out var nonStaticUrl))
                                _nonStaticApiUrl = nonStaticUrl.ToString() ?? _nonStaticApiUrl;

                            _logger.LogInformation("SunHotels API URLs loaded from settings - Static: {StaticUrl}, NonStatic: {NonStaticUrl}",
                                _staticApiUrl, _nonStaticApiUrl);
                        }
                    }
                    catch (Exception settingsEx)
                    {
                        _logger.LogWarning(settingsEx, "Failed to parse SunHotels settings JSON, using default URLs");
                    }
                }

                _logger.LogInformation("SunHotels configuration loaded from database for service: {ServiceName}", dbConfig.ServiceName);
            }
            else
            {
                _logger.LogWarning("SunHotels configuration not found in database, using default config");
            }

            _configLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load SunHotels configuration from database, using default config");
            _configLoaded = true; // Don't try to load again
        }
        finally
        {
            _configLoadLock.Release();
        }
    }

    #region Static Data Methods

    public async Task<List<SunHotelsDestination>> GetDestinationsAsync(string? language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        // ✅ Redis cache kontrolü
        var cachedDestinations = await _redisCache.GetDestinationsAsync(language ?? "en", cancellationToken);
        if (cachedDestinations != null)
        {
            _logger.LogInformation("Destinations loaded from Redis cache (Language: {Language})", language);
            return cachedDestinations;
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language ?? "en" },
                { "destinationCode", "" },
                { "sortBy", "" },
                { "sortOrder", "" },
                { "exactDestinationMatch", "" }
            };

            var response = await SendStaticRequestAsync("GetDestinations", parameters, cancellationToken);
            var destinations = ParseDestinations(response);

            // ✅ Redis'e kaydet
            await _redisCache.SetDestinationsAsync(destinations, language ?? "en", cancellationToken);

            return destinations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting destinations from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get destinations", ex);
        }
    }

    public async Task<List<SunHotelsResort>> GetResortsAsync(string? destinationId = null, string? language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        // ✅ Redis cache kontrolü
        var cachedResorts = await _redisCache.GetResortsAsync(destinationId, language ?? "en", cancellationToken);
        if (cachedResorts != null)
        {
            _logger.LogInformation("Resorts loaded from Redis cache (DestinationId: {DestinationId}, Language: {Language})",
                destinationId ?? "all", language);
            return cachedResorts;
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language ?? "en" },
                { "destinationID", destinationId ?? "" },
                { "destinationCode", "" },
                { "sortBy", "" },
                { "sortOrder", "" },
                { "exactDestinationMatch", "" }
            };

            var response = await SendStaticRequestAsync("GetResorts", parameters, cancellationToken);
            var resorts = ParseResorts(response);

            // ✅ Redis'e kaydet
            await _redisCache.SetResortsAsync(resorts, destinationId, language ?? "en", cancellationToken);

            return resorts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resorts from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get resorts", ex);
        }
    }

    public async Task<List<SunHotelsMeal>> GetMealsAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", NonStaticLanguage }
            };

            var response = await SendStaticRequestAsync("GetMeals", parameters, cancellationToken);
            return ParseMeals(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meals from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get meals", ex);
        }
    }

    public async Task<List<SunHotelsRoomType>> GetRoomTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", NonStaticLanguage }
            };

            var response = await SendStaticRequestAsync("GetRoomTypes", parameters, cancellationToken);
            return ParseRoomTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get room types", ex);
        }
    }

    public async Task<List<SunHotelsFeature>> GetFeaturesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetFeatures", parameters, cancellationToken);
            return ParseFeatures(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting features from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get features", ex);
        }
    }

    public async Task<List<SunHotelsLanguage>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var response = await SendStaticRequestAsync("GetLanguages", new Dictionary<string, string>(), cancellationToken);
            return ParseLanguages(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting languages from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get languages", ex);
        }
    }

    public async Task<List<SunHotelsTheme>> GetThemesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var response = await SendStaticRequestAsync("GetThemes", new Dictionary<string, string>(), cancellationToken);
            return ParseThemes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting themes from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get themes", ex);
        }
    }

    public async Task<List<SunHotelsTransferType>> GetTransferTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language },
                { "transferTypeCode", "" }
            };

            var response = await SendStaticRequestAsync("GetTransferTypes", parameters, cancellationToken);
            return ParseTransferTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get transfer types", ex);
        }
    }

    public async Task<List<SunHotelsNoteType>> GetHotelNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetHotelNoteTypes", parameters, cancellationToken);
            return ParseNoteTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel note types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get hotel note types", ex);
        }
    }

    public async Task<List<SunHotelsNoteType>> GetRoomNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            var response = await SendStaticRequestAsync("GetRoomNoteTypes", parameters, cancellationToken);
            return ParseNoteTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room note types from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get room note types", ex);
        }
    }

    public async Task<List<SunHotelsStaticHotel>> GetStaticHotelsAndRoomsAsync(
        string? destination = null,
        string? hotelIds = null,
        string? resortIds = null,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        // ✅ Independent CTS: Do NOT link to request token - static fetch should complete
        // even if client disconnects, so the result can be cached for subsequent requests
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        try
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Getting static hotels and rooms - Destination: {Destination}, HotelIds: {HotelIds}, ResortIds: {ResortIds}, Language: {Language}",
                destination ?? "null", hotelIds ?? "null", resortIds ?? "null", language);

            var parameters = new Dictionary<string, string>
            {
                { "language", language },
                { "destination", destination ?? string.Empty },  // ✅ API requires destination even if empty
                { "hotelIds", hotelIds ?? string.Empty },
                { "resortIds", resortIds ?? string.Empty },
                { "accommodationTypes", string.Empty },
                { "sortBy", string.Empty },
                { "sortOrder", string.Empty },
                { "exactDestinationMatch", string.Empty }
            };

            var response = await SendStaticRequestAsync("GetStaticHotelsAndRooms", parameters, cts.Token);
            var hotels = ParseStaticHotels(response);

            _logger.LogInformation("Successfully retrieved {Count} hotels from SunHotels in {ElapsedMs} ms", hotels.Count, sw.ElapsedMilliseconds);
            return hotels;
        }
        catch (TaskCanceledException tce)
        {
            _logger.LogWarning(tce, "SunHotels static fetch timed out or was canceled (timeout 90s, userCanceled={UserCanceled})", cancellationToken.IsCancellationRequested);
            throw new ExternalServiceException("SunHotels", "Static fetch canceled or timed out", tce);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting static hotels from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get static hotels", ex);
        }
    }

    #endregion

    #region Search Methods

    public async Task<List<SunHotelsSearchResult>> SearchHotelsAsync(SunHotelsSearchRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "destination", request.DestinationId },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "numberOfAdults", request.Adults.ToString() },
                { "numberOfChildren", request.Children.ToString() },
                { "currency", request.Currency }
            };

            var response = await SendNonStaticRequestAsync("search", parameters, cancellationToken);
            return ParseSearchResults(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hotels from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to search hotels", ex);
        }
    }

    public async Task<List<SunHotelsSearchResultV3>> SearchHotelsV3Async(SunHotelsSearchRequestV3 request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        // ✅ Redis cache kontrolü (sadece temel aramalar için - filtreler olmadan)
        // Not: HotelIds, ResortIds, ThemeIds gibi filtreler varsa cache kullanma
        var useCache = string.IsNullOrEmpty(request.HotelIds) &&
                      string.IsNullOrEmpty(request.ResortIds) &&
                      string.IsNullOrEmpty(request.ThemeIds) &&
                      string.IsNullOrEmpty(request.FeatureIds) &&
                      !request.MinPrice.HasValue &&
                      !request.MaxPrice.HasValue;

        if (useCache)
        {
            var cachedResults = await _redisCache.GetHotelSearchAsync(
                request.DestinationId,
                request.CheckIn,
                request.CheckOut,
                request.Adults,
                request.Children,
                cancellationToken);

            if (cachedResults != null && cachedResults.Any())
            {
                _logger.LogInformation("Hotel search results loaded from Redis cache ({Count} hotels)", cachedResults.Count);
                return cachedResults;
            }
        }

        try
        {
            var checkInFormatted = request.CheckIn.ToString("yyyy-MM-dd");
            var checkOutFormatted = request.CheckOut.ToString("yyyy-MM-dd");

            _logger.LogInformation("SearchHotelsV3Async - DestinationId: {DestinationId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}, Adults: {Adults}, Children: {Children}",
                request.DestinationId, checkInFormatted, checkOutFormatted, request.Adults, request.Children);

            var parameters = new Dictionary<string, string>
            {
                { "destinationID", request.DestinationId },  // Büyük harf ID
                { "checkInDate", checkInFormatted },
                { "checkOutDate", checkOutFormatted },
                { "numberOfRooms", request.NumberOfRooms.ToString() },
                { "numberOfAdults", request.Adults.ToString() },
                { "numberOfChildren", request.Children.ToString() },
                { "infant", request.Infant.ToString() },
                { "currency", NonStaticCurrency },
                { "currencies", NonStaticCurrency }, // Required parameter
                { "language", NonStaticLanguage },
                { "b2c", request.B2C ? "1" : "0" },
                { "showCoordinates", request.ShowCoordinates ? "1" : "0" },
                { "showReviews", request.ShowReviews ? "1" : "0" },
                { "showRoomTypeName", request.ShowRoomTypeName ? "1" : "0" },
                { "excludeSharedRooms", request.ExcludeSharedRooms ? "1" : "0" },
                { "excludeSharedFacilities", request.ExcludeSharedFacilities ? "1" : "0" }
            };

            if (!string.IsNullOrEmpty(request.ChildrenAges))
                parameters.Add("childrenAges", request.ChildrenAges);
            if (!string.IsNullOrEmpty(request.HotelIds))
                parameters.Add("hotelIDs", request.HotelIds);  // Büyük harf IDs
            if (!string.IsNullOrEmpty(request.ResortIds))
                parameters.Add("resortIDs", request.ResortIds);  // Büyük harf IDs
            if (!string.IsNullOrEmpty(request.MealIds))
                parameters.Add("mealIds", request.MealIds);
            if (!string.IsNullOrEmpty(request.FeatureIds))
                parameters.Add("featureIds", request.FeatureIds);
            if (!string.IsNullOrEmpty(request.ThemeIds))
                parameters.Add("themeIds", request.ThemeIds);
            if (request.MinStarRating.HasValue)
                parameters.Add("minStarRating", request.MinStarRating.Value.ToString());
            if (request.MaxStarRating.HasValue)
                parameters.Add("maxStarRating", request.MaxStarRating.Value.ToString());
            if (request.MinPrice.HasValue)
                parameters.Add("minPrice", request.MinPrice.Value.ToString(CultureInfo.InvariantCulture));
            if (request.MaxPrice.HasValue)
                parameters.Add("maxPrice", request.MaxPrice.Value.ToString(CultureInfo.InvariantCulture));
            if (request.ReferenceLatitude.HasValue && request.ReferenceLongitude.HasValue)
            {
                parameters.Add("referencePointLatitude", request.ReferenceLatitude.Value.ToString(CultureInfo.InvariantCulture));
                parameters.Add("referencePointLongitude", request.ReferenceLongitude.Value.ToString(CultureInfo.InvariantCulture));
                if (request.MaxDistanceKm.HasValue)
                    parameters.Add("maxDistanceFromReferencePoint", request.MaxDistanceKm.Value.ToString());
            }
            if (!string.IsNullOrEmpty(request.CustomerCountry))
                parameters.Add("customerCountry", request.CustomerCountry);
            if (request.PaymentMethodId.HasValue)
                parameters.Add("paymentMethodId", request.PaymentMethodId.Value.ToString());
            // Accommodation types (hotel, apartment, villa, resort). If provided, include in parameters.
            if (!string.IsNullOrEmpty(request.AccommodationTypes))
                parameters.Add("accommodationTypes", request.AccommodationTypes);

            var response = await SendNonStaticRequestAsync("searchV3", parameters, cancellationToken);

            // ✅ FIX 1: Error tag kontrolü (Python referans: if "<Error>" in response.text)
            if (response.Contains("<Error>"))
            {
                _logger.LogError("SunHotels SearchV3 returned error. Checking for error details...");

                // XML'den error mesajını parse et
                try
                {
                    var errorDoc = XDocument.Parse(response);
                    var errorMsg = errorDoc.Descendants("Error").FirstOrDefault()?.Value;
                    _logger.LogError("SunHotels API Error: {ErrorMessage}", errorMsg ?? "Unknown error");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not parse error XML");
                }

                // Boş sonuç döndür (fallback sample hotels eklenebilir)
                return new List<SunHotelsSearchResultV3>();
            }

            var results = ParseSearchResultsV3(response);
            _logger.LogInformation("SearchHotelsV3Async - Parsed {Count} hotels from response", results.Count);

            // ✅ FIX 2: Boş sonuç fallback (Python referans: if len(hotels) == 0: return sample_hotels)
            if (results.Count == 0)
            {
                _logger.LogWarning("SunHotels SearchV3 returned 0 results for DestinationId: {DestinationId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}",
                    request.DestinationId, request.CheckIn.ToString("yyyy-MM-dd"), request.CheckOut.ToString("yyyy-MM-dd"));

                // TODO: Gelecekte sample hotels eklenebilir (Vienna, Paris, Barcelona gibi popüler destinasyonlar)
                // return GetSampleHotels(request);
            }

            // Static datayı cache'den al ve doldur
            // ✅ Locale göz ardı: Her zaman İngilizce (Python referansı)
            await EnrichHotelsWithStaticDataAsync(results, "en", cancellationToken);

            // ✅ Redis'e kaydet (sadece filtresiz aramalar için)
            if (useCache && results.Any())
            {
                await _redisCache.SetHotelSearchAsync(
                    results,
                    request.DestinationId,
                    request.CheckIn,
                    request.CheckOut,
                    request.Adults,
                    request.Children,
                    cancellationToken);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hotels V3 from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to search hotels V3", ex);
        }
    }

    /// <summary>
    /// Otel detaylarını getirir. ÖNEMLİ: destinationId veya resortId parametrelerinden en az biri verilmelidir.
    /// Python server.py referansı: destinationID ile aramak, sadece hotelID ile aramaktan daha iyi sonuçlar verir.
    /// </summary>
    public async Task<SunHotelsSearchResultV3?> GetHotelDetailsAsync(
        int hotelId,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        int children = 0,
        string currency = "EUR",
        string? destinationId = null,
        string? resortId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SunHotelsSearchRequestV3
            {
                CheckIn = checkIn,
                CheckOut = checkOut,
                Adults = adults,
                Children = children,
                Currency = currency,
                Language = "en", // ✅ Sabit İngilizce
                ShowCoordinates = true,
                ShowReviews = true,
                ShowRoomTypeName = true
            };

            // API sadece destination, destinationID, hotelIDs veya resortIDs'den BİRİNİ kabul eder
            // Python mantığı: destinationID + hotelID filter > resortID + hotelID filter > sadece hotelID
            // destinationID kullanmak daha iyi sonuçlar verir!
            if (!string.IsNullOrEmpty(destinationId))
            {
                request.DestinationId = destinationId;
                request.ResortIds = "";
                request.HotelIds = ""; // API destinationID ile arama yapar, sonuçları hotelId'ye göre filtreleriz
                _logger.LogInformation("Hotel details search: using destinationID={DestinationId}, will filter for hotel_id={HotelId}", destinationId, hotelId);
            }
            else if (!string.IsNullOrEmpty(resortId))
            {
                request.DestinationId = "";
                request.ResortIds = resortId;
                request.HotelIds = ""; // API resortID ile arama yapar, sonuçları hotelId'ye göre filtreleriz
                _logger.LogInformation("Hotel details search: using resortID={ResortId}, will filter for hotel_id={HotelId}", resortId, hotelId);
            }
            else
            {
                // ✅ FALLBACK: Cache'ten otelin destinationId veya resortId'sini bulmaya çalış
                _logger.LogInformation("Hotel details search: no destination/resort provided, looking up from static cache for hotel_id={HotelId}", hotelId);

                try
                {
                    // Önce otel cache'inden oteli al
                    var cachedHotel = await _cacheService.GetHotelByIdAsync(hotelId, "en", cancellationToken);

                    if (cachedHotel != null && cachedHotel.ResortId > 0)
                    {
                        // Resort'tan destinationId'yi al (daha iyi sonuçlar verir)
                        var cachedResort = await _cacheService.GetResortByIdAsync(cachedHotel.ResortId, "en", cancellationToken);

                        if (cachedResort != null && !string.IsNullOrEmpty(cachedResort.DestinationId))
                        {
                            // DestinationId varsa onu kullan (en iyi yöntem)
                            request.DestinationId = cachedResort.DestinationId;
                            request.ResortIds = "";
                            request.HotelIds = "";
                            _logger.LogInformation("Found destinationID={DestinationId} from static cache (via resort {ResortId}) for hotel_id={HotelId}",
                                cachedResort.DestinationId, cachedHotel.ResortId, hotelId);
                        }
                        else
                        {
                            // DestinationId yoksa resortId kullan
                            request.DestinationId = "";
                            request.ResortIds = cachedHotel.ResortId.ToString();
                            request.HotelIds = "";
                            _logger.LogInformation("Found resortID={ResortId} from static cache for hotel_id={HotelId} (no destinationId available)",
                                cachedHotel.ResortId, hotelId);
                        }
                    }
                    else
                    {
                        // Son çare: sadece hotelID (bazı oteller için boş sonuç dönebilir)
                        request.DestinationId = "";
                        request.ResortIds = "";
                        request.HotelIds = hotelId.ToString();
                        _logger.LogWarning("Hotel {HotelId} not found in static cache or no resortId, using hotelID only (may return empty)", hotelId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting hotel from static cache, falling back to hotelID only");
                    request.DestinationId = "";
                    request.ResortIds = "";
                    request.HotelIds = hotelId.ToString();
                }
            }

            var results = await SearchHotelsV3Async(request, cancellationToken);

            // destinationId veya resortId ile arama yaptıysak, sonuçları hotelId'ye göre filtrele
            if (!string.IsNullOrEmpty(destinationId) || !string.IsNullOrEmpty(resortId))
            {
                var filteredResult = results.FirstOrDefault(h => h.HotelId == hotelId);
                if (filteredResult == null)
                {
                    _logger.LogWarning("Hotel {HotelId} not found in results after filtering with destinationId={DestinationId}/resortId={ResortId}. Trying cache...", hotelId, destinationId, resortId);

                    // Boş oda durumunda cache'den otel bilgisini al ve boş oda listesiyle döndür
                    try
                    {
                        var cachedHotels = await _cacheService.GetAllHotelsAsync("en", cancellationToken);
                        var cachedHotel = cachedHotels.FirstOrDefault(h => h.HotelId == hotelId);
                        if (cachedHotel != null)
                        {
                            _logger.LogInformation("Hotel {HotelId} found in cache with no available rooms for selected dates", hotelId);

                            // Cache'teki oteli SearchV3'ün döneceği formata çevir (oda listesi boş)
                            var hotelWithNoRooms = new SunHotelsSearchResultV3
                            {
                                HotelId = cachedHotel.HotelId,
                                Name = cachedHotel.Name,
                                Description = cachedHotel.Description,
                                Category = cachedHotel.Category,
                                City = cachedHotel.City,
                                Country = cachedHotel.Country,
                                CountryCode = cachedHotel.CountryCode,
                                Address = cachedHotel.Address,
                                ResortId = cachedHotel.ResortId,
                                ResortName = cachedHotel.ResortName,
                                Latitude = cachedHotel.Latitude ?? 0,
                                Longitude = cachedHotel.Longitude ?? 0,
                                ImageUrls = (JsonSerializer.Deserialize<List<string>>(cachedHotel.ImageUrls) ?? new List<string>()),
                                Currency = request.Currency,
                                MinPrice = 0,
                                Rooms = new List<SunHotelsRoomV3>() // Boş oda listesi
                            };

                            // FeatureIds ve ThemeIds string'i int listesine çevir
                            var featureIds = JsonSerializer.Deserialize<List<int>>(cachedHotel.FeatureIds) ?? new List<int>();
                            var themeIds = JsonSerializer.Deserialize<List<int>>(cachedHotel.ThemeIds) ?? new List<int>();
                            hotelWithNoRooms.FeatureIds = featureIds;
                            hotelWithNoRooms.ThemeIds = themeIds;

                            return hotelWithNoRooms;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not load hotel from cache for {HotelId}", hotelId);
                    }

                    return null;
                }
                // Boş oda durumunda otel bilgisini döndür (Rooms boş list olacak)
                return filteredResult;
            }

            // Sadece hotelIds ile arama yapıldıysa ilk (ve tek) sonucu döndür
            var result = results.FirstOrDefault();
            // Boş oda durumunda da otel bilgisini döndür, null döndürme
            if (result == null)
            {
                _logger.LogWarning("Hotel {HotelId} not found in search results, trying cache fallback", hotelId);

                // HotelIds-only search'te boş sonuç dönerse, cache'ten otel al
                try
                {
                    var cachedHotels = await _cacheService.GetAllHotelsAsync("en", cancellationToken);
                    var cachedHotel = cachedHotels.FirstOrDefault(h => h.HotelId == hotelId);
                    if (cachedHotel != null)
                    {
                        _logger.LogInformation("Hotel {HotelId} found in cache as fallback for hotelIds-only search", hotelId);

                        // Cache'teki oteli SearchV3'ün döneceği formata çevir (oda listesi boş)
                        var hotelWithNoRooms = new SunHotelsSearchResultV3
                        {
                            HotelId = cachedHotel.HotelId,
                            Name = cachedHotel.Name,
                            Description = cachedHotel.Description,
                            Category = cachedHotel.Category,
                            City = cachedHotel.City,
                            Country = cachedHotel.Country,
                            CountryCode = cachedHotel.CountryCode,
                            Address = cachedHotel.Address,
                            ResortId = cachedHotel.ResortId,
                            ResortName = cachedHotel.ResortName,
                            Latitude = cachedHotel.Latitude ?? 0,
                            Longitude = cachedHotel.Longitude ?? 0,
                            ImageUrls = (JsonSerializer.Deserialize<List<string>>(cachedHotel.ImageUrls) ?? new List<string>()),
                            Currency = request.Currency,
                            MinPrice = 0,
                            Rooms = new List<SunHotelsRoomV3>() // Boş oda listesi
                        };

                        // FeatureIds ve ThemeIds string'i int listesine çevir
                        var featureIds = JsonSerializer.Deserialize<List<int>>(cachedHotel.FeatureIds) ?? new List<int>();
                        var themeIds = JsonSerializer.Deserialize<List<int>>(cachedHotel.ThemeIds) ?? new List<int>();
                        hotelWithNoRooms.FeatureIds = featureIds;
                        hotelWithNoRooms.ThemeIds = themeIds;

                        return hotelWithNoRooms;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load hotel from cache for hotelIds-only search for {HotelId}", hotelId);
                }

                return null;
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel details from SunHotels for hotel {HotelId}", hotelId);
            throw new ExternalServiceException("SunHotels", $"Failed to get hotel details for {hotelId}", ex);
        }
    }

    #endregion

    #region PreBook Methods

    public async Task<SunHotelsPreBookResult> PreBookAsync(SunHotelsPreBookRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "hotelId", request.HotelId },
                { "roomId", request.RoomId },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "numberOfAdults", request.Adults.ToString() },
                { "numberOfChildren", request.Children.ToString() }
            };

            var response = await SendNonStaticRequestAsync("preBook", parameters, cancellationToken);
            return ParsePreBookResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pre-booking from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to pre-book", ex);
        }
    }

    public async Task<SunHotelsPreBookResultV3> PreBookV3Async(SunHotelsPreBookRequestV3 request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            // SunHotels PreBookV3 API (Python backend'den alınan çalışan mantık):
            // roomId kullanılırken hotelId, roomtypeId, blockSuperDeal BOŞ STRING olarak gönderilmeli
            // searchPrice CENT formatında olmalı (örn: 100.50 EUR -> 10050)

            _logger.LogInformation("PreBookV3 request - HotelId: {HotelId}, RoomId: {RoomId}, RoomTypeId: {RoomTypeId}",
                request.HotelId, request.RoomId, request.RoomTypeId);

            // searchPrice'ı cent'e çevir (Python'daki gibi: int(search_price * 100))
            var searchPriceInCents = (int)Math.Round(request.SearchPrice * 100);

            var parameters = new Dictionary<string, string>
            {
                { "currency", NonStaticCurrency },
                { "language", NonStaticLanguage },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "rooms", request.Rooms.ToString() },
                { "adults", request.Adults.ToString() },
                { "children", request.Children.ToString() },
                { "childrenAges", request.ChildrenAges ?? "" },
                { "infant", request.Infant.ToString() },
                { "mealId", request.MealId.ToString() },
                { "customerCountry", request.CustomerCountry ?? "NL" },
                { "b2c", request.B2C ? "1" : "0" },
                { "searchPrice", searchPriceInCents.ToString() },
                { "roomId", request.RoomId.ToString() },
                { "hotelId", "" },           // BOŞ - roomId kullanılırken
                { "roomtypeId", "" },        // BOŞ - roomId kullanılırken  
                { "blockSuperDeal", "" },    // BOŞ - roomId kullanılırken
                { "showPriceBreakdown", "1" }
            };

            if (request.PaymentMethodId > 0)
                parameters.Add("paymentMethodId", request.PaymentMethodId.ToString());

            _logger.LogInformation("PreBookV3 sending (Python style): roomId={RoomId}, hotelId='', roomtypeId='', blockSuperDeal='', searchPrice={SearchPrice} cents",
                request.RoomId, searchPriceInCents);

            var response = await SendNonStaticRequestAsync("PreBookV3", parameters, cancellationToken);
            return ParsePreBookResultV3(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pre-booking V3 from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to pre-book V3", ex);
        }
    }

    #endregion

    #region Book Methods

    public async Task<SunHotelsBookResult> BookAsync(SunHotelsBookRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "preBookCode", request.PreBookCode },
                { "guestName", request.GuestName },
                { "guestEmail", request.GuestEmail },
                { "guestPhone", request.GuestPhone ?? "" },
                { "specialRequests", request.SpecialRequests ?? "" }
            };

            var response = await SendNonStaticRequestAsync("book", parameters, cancellationToken);
            return ParseBookResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to complete booking", ex);
        }
    }

    public async Task<SunHotelsBookResultV3> BookV3Async(SunHotelsBookRequestV3 request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        // PreBookCode zorunlu - boş ise API eski session kullanır ve hatalı parametre ister
        if (string.IsNullOrWhiteSpace(request.PreBookCode))
        {
            throw new ArgumentException("PreBookCode boş olamaz. Önce PreBookV3 yapılmalı.", nameof(request.PreBookCode));
        }

        try
        {
            _logger.LogInformation("BookV3 Request: PreBookCode={PreBookCode}, Adults={Adults}, Children={Children}, AdultGuestCount={AdultGuestCount}, ChildGuestCount={ChildGuestCount}",
                request.PreBookCode, request.Adults, request.Children, request.AdultGuests.Count, request.ChildrenGuests.Count);

            // PaymentMethodId: 1 = Invoice, 2 = Pay at hotel (0 kabul edilmiyor)
            var paymentMethodId = request.PaymentMethodId > 0 ? request.PaymentMethodId : 1; // Default: Invoice

            var parameters = new Dictionary<string, string>
            {
                { "preBookCode", request.PreBookCode },
                { "roomId", request.RoomId.ToString() },
                { "mealId", request.MealId.ToString() },
                { "checkInDate", request.CheckIn.ToString("yyyy-MM-dd") },
                { "checkOutDate", request.CheckOut.ToString("yyyy-MM-dd") },
                { "rooms", request.Rooms.ToString() },
                { "adults", request.Adults.ToString() },
                { "children", request.Children.ToString() },
                { "infant", request.Infant.ToString() },
                { "currency", NonStaticCurrency },
                { "language", NonStaticLanguage },
                { "email", request.Email },
                { "yourRef", request.YourRef ?? "FREESTAYS" },
                { "specialRequest", request.SpecialRequest ?? "" },
                { "b2c", request.B2C ? "1" : "0" },
                { "paymentMethodId", paymentMethodId.ToString() },
                // SunHotels API tüm parametreleri zorunlu olarak bekler - boş olsalar bile gönderilmeli
                { "customerCountry", request.CustomerCountry ?? "" },
                { "invoiceRef", request.InvoiceRef ?? "" },
                { "commissionAmountInHotelCurrency", request.CommissionAmount?.ToString(CultureInfo.InvariantCulture) ?? "" },
                { "customerEmail", request.CustomerEmail ?? request.Email } // customerEmail yoksa email kullan
            };

            // Add adult guests - SunHotels API her zaman 9 yetişkin parametresi bekler
            // Kullanılmayanlar için boş string gönderilmeli
            _logger.LogInformation("BookV3: request.Adults={Adults}, AdultGuests.Count={Count}", request.Adults, request.AdultGuests.Count);

            // SunHotels API'si oda kapasitesine göre parametre sayısı bekleyebilir
            // Güvenli yaklaşım: 9 yetişkin için parametre gönder (kullanılmayanlar boş string)
            for (int i = 0; i < 9; i++)
            {
                if (i < request.AdultGuests.Count)
                {
                    // Gerçek guest bilgisi var
                    var guest = request.AdultGuests[i];
                    parameters.Add($"adultGuest{i + 1}FirstName", guest.FirstName ?? "Guest");
                    parameters.Add($"adultGuest{i + 1}LastName", guest.LastName ?? "User");
                }
                else if (i < request.Adults)
                {
                    // Yeterli guest bilgisi yok ama Adults sayısı içinde - dummy guest ekle
                    parameters.Add($"adultGuest{i + 1}FirstName", "Guest");
                    parameters.Add($"adultGuest{i + 1}LastName", $"User{i + 1}");
                    _logger.LogWarning("BookV3: adultGuest{Index} için bilgi yok, dummy değer kullanılıyor", i + 1);
                }
                else
                {
                    // Adults sayısının dışında - boş string gönder (SunHotels API'nin beklediği davranış)
                    parameters.Add($"adultGuest{i + 1}FirstName", "");
                    parameters.Add($"adultGuest{i + 1}LastName", "");
                }
            }

            // Add child guests - SunHotels API 9 çocuk parametresi bekler
            // API format: childrenGuest1FirstName, childrenGuest1LastName, childrenGuestAge1
            for (int i = 0; i < 9; i++)
            {
                if (i < request.ChildrenGuests.Count)
                {
                    var child = request.ChildrenGuests[i];
                    parameters.Add($"childrenGuest{i + 1}FirstName", child.FirstName ?? "Child");
                    parameters.Add($"childrenGuest{i + 1}LastName", child.LastName ?? "User");
                    parameters.Add($"childrenGuestAge{i + 1}", child.Age.ToString());
                }
                else if (i < request.Children)
                {
                    // Children sayısı içinde ama bilgi yok - dummy değer
                    parameters.Add($"childrenGuest{i + 1}FirstName", "Child");
                    parameters.Add($"childrenGuest{i + 1}LastName", $"User{i + 1}");
                    parameters.Add($"childrenGuestAge{i + 1}", "5"); // Varsayılan yaş
                    _logger.LogWarning("BookV3: childrenGuest{Index} için bilgi yok, dummy değer kullanılıyor", i + 1);
                }
                else
                {
                    // Children sayısının dışında - boş string gönder
                    parameters.Add($"childrenGuest{i + 1}FirstName", "");
                    parameters.Add($"childrenGuest{i + 1}LastName", "");
                    parameters.Add($"childrenGuestAge{i + 1}", "");
                }
            }

            // Credit card parametreleri - SunHotels API her zaman bu parametreleri bekler (boş olsalar bile)
            if (request.CreditCard != null)
            {
                parameters.Add("creditCardType", request.CreditCard.CardType ?? "");
                parameters.Add("creditCardNumber", request.CreditCard.CardNumber ?? "");
                parameters.Add("creditCardHolder", request.CreditCard.CardHolder ?? "");
                parameters.Add("creditCardCVV2", request.CreditCard.CVV ?? "");
                parameters.Add("creditCardExpYear", request.CreditCard.ExpYear ?? "");
                parameters.Add("creditCardExpMonth", request.CreditCard.ExpMonth ?? "");
            }
            else
            {
                // Credit card bilgisi yok - boş parametreler gönder
                parameters.Add("creditCardType", "");
                parameters.Add("creditCardNumber", "");
                parameters.Add("creditCardHolder", "");
                parameters.Add("creditCardCVV2", "");
                parameters.Add("creditCardExpYear", "");
                parameters.Add("creditCardExpMonth", "");
            }

            // Log tüm adult guest parametrelerini
            _logger.LogInformation("BookV3 Parameters - PreBookCode={PreBookCode}, Adults={Adults}, adultGuest parametreleri:",
                request.PreBookCode, request.Adults);
            foreach (var param in parameters.Where(p => p.Key.Contains("adultGuest")))
            {
                _logger.LogInformation("  {Key}={Value}", param.Key, param.Value);
            }

            var response = await SendNonStaticRequestAsync("BookV3", parameters, cancellationToken);
            var result = ParseBookResultV3(response);

            // SunHotels hata döndürdüyse exception fırlat
            if (!result.Success || string.IsNullOrEmpty(result.BookingNumber))
            {
                var errorMessage = result.ErrorMessage ?? "Booking failed - no booking number returned";
                _logger.LogError("SunHotels BookV3 failed - ErrorMessage: {ErrorMessage}", errorMessage);
                throw new ExternalServiceException("SunHotels", $"Booking failed: {errorMessage}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking V3 from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to complete booking V3", ex);
        }
    }

    public async Task<SunHotelsCancelResult> CancelBookingAsync(string bookingId, string language = "en", CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingID", bookingId },
                { "language", language }
            };

            var response = await SendNonStaticRequestAsync("CancelBooking", parameters, cancellationToken);
            return ParseCancelResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking from SunHotels for booking {BookingId}", bookingId);
            throw new ExternalServiceException("SunHotels", $"Failed to cancel booking {bookingId}", ex);
        }
    }

    public async Task<List<SunHotelsBookingInfo>> GetBookingInformationAsync(SunHotelsGetBookingRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", NonStaticLanguage },
                { "showGuests", request.ShowGuests ? "1" : "0" }
            };

            if (!string.IsNullOrEmpty(request.BookingId))
                parameters.Add("bookingnumber", request.BookingId);
            if (!string.IsNullOrEmpty(request.Reference))
                parameters.Add("yourRef", request.Reference);
            if (request.CreatedDateFrom.HasValue)
                parameters.Add("createdDateFrom", request.CreatedDateFrom.Value.ToString("yyyy-MM-dd"));
            if (request.CreatedDateTo.HasValue)
                parameters.Add("createdDateTo", request.CreatedDateTo.Value.ToString("yyyy-MM-dd"));
            if (request.ArrivalDateFrom.HasValue)
                parameters.Add("arrivalDateFrom", request.ArrivalDateFrom.Value.ToString("yyyy-MM-dd"));
            if (request.ArrivalDateTo.HasValue)
                parameters.Add("arrivalDateTo", request.ArrivalDateTo.Value.ToString("yyyy-MM-dd"));

            var response = await SendNonStaticRequestAsync("getBookingInformationV3", parameters, cancellationToken);
            return ParseBookingInformation(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking information from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get booking information", ex);
        }
    }

    #endregion

    #region Amendment Methods

    public async Task<SunHotelsAmendmentPriceResult> GetAmendmentPriceAsync(SunHotelsAmendmentPriceRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConfigLoadedAsync(cancellationToken);

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", request.BookingId },
                { "newCheckInDate", request.NewCheckIn.ToString("yyyy-MM-dd") },
                { "newCheckOutDate", request.NewCheckOut.ToString("yyyy-MM-dd") },
                { "language", NonStaticLanguage }
            };

            var response = await SendNonStaticRequestAsync("amendmentPriceRequest", parameters, cancellationToken);
            return ParseAmendmentPriceResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting amendment price from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get amendment price", ex);
        }
    }

    public async Task<SunHotelsAmendmentResult> AmendBookingAsync(SunHotelsAmendmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", request.BookingId },
                { "newCheckInDate", request.NewCheckIn.ToString("yyyy-MM-dd") },
                { "newCheckOutDate", request.NewCheckOut.ToString("yyyy-MM-dd") },
                { "maxPrice", request.MaxPrice.ToString(CultureInfo.InvariantCulture) },
                { "language", NonStaticLanguage },
                { "bookingType", request.BookingType }
            };

            if (request.NewRoomId.HasValue)
                parameters.Add("newRoomId", request.NewRoomId.Value.ToString());

            var response = await SendNonStaticRequestAsync("amendmentRequest", parameters, cancellationToken);
            return ParseAmendmentResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error amending booking from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to amend booking", ex);
        }
    }

    #endregion

    #region Special Request Methods

    public async Task<SunHotelsSpecialRequestResult> UpdateSpecialRequestAsync(string bookingId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", bookingId },
                { "specialRequest", text }
            };

            var response = await SendNonStaticRequestAsync("updateSpecialRequest", parameters, cancellationToken);
            return ParseSpecialRequestResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating special request from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to update special request", ex);
        }
    }

    public async Task<SunHotelsSpecialRequestResult> GetSpecialRequestAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", bookingId }
            };

            var response = await SendNonStaticRequestAsync("getSpecialRequest", parameters, cancellationToken);
            return ParseSpecialRequestResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting special request from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get special request", ex);
        }
    }

    #endregion

    #region Transfer Methods

    public async Task<List<SunHotelsTransferSearchResult>> SearchTransfersAsync(SunHotelsTransferSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "arrivalDate", request.ArrivalDate.ToString("yyyy-MM-dd") },
                { "arrivalTime", request.ArrivalTime },
                { "currency", NonStaticCurrency },
                { "language", NonStaticLanguage }
            };

            if (request.HotelId.HasValue)
                parameters.Add("hotelId", request.HotelId.Value.ToString());
            if (request.RoomId.HasValue)
                parameters.Add("roomId", request.RoomId.Value.ToString());
            if (!string.IsNullOrEmpty(request.BookingId))
                parameters.Add("bookingnumber", request.BookingId);
            if (request.ResortId.HasValue)
                parameters.Add("resortId", request.ResortId.Value.ToString());
            if (request.TransferId.HasValue)
                parameters.Add("transferId", request.TransferId.Value.ToString());
            if (!string.IsNullOrEmpty(request.GiataCode))
                parameters.Add("giataCode", request.GiataCode);
            if (request.ReturnDepartureDate.HasValue)
                parameters.Add("returnDepartureDate", request.ReturnDepartureDate.Value.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrEmpty(request.ReturnDepartureTime))
                parameters.Add("returnDepartureTime", request.ReturnDepartureTime);

            var response = await SendNonStaticRequestAsync("searchTransfersV2", parameters, cancellationToken);
            return ParseTransferSearchResults(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transfers from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to search transfers", ex);
        }
    }

    public async Task<SunHotelsAddTransferResult> AddTransferAsync(SunHotelsAddTransferRequestV2 request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "bookingnumber", request.BookingId },
                { "hotelName", request.HotelName },
                { "contactPerson", request.ContactPerson },
                { "contactCellphone", request.ContactCellphone },
                { "airlineCruiseline", request.AirlineCruiseline },
                { "flightNumber", request.FlightNumber },
                { "departureTime", request.DepartureTime },
                { "arrivalTime", request.ArrivalTime },
                { "arrivalDate", request.ArrivalDate.ToString("yyyy-MM-dd") },
                { "passengers", request.Passengers.ToString() },
                { "transferId", request.TransferId.ToString() },
                { "returnTransfer", request.ReturnTransfer ? "1" : "0" },
                { "currency", NonStaticCurrency },
                { "language", NonStaticLanguage },
                { "email", request.Email }
            };

            if (request.HotelId.HasValue)
                parameters.Add("hotelId", request.HotelId.Value.ToString());
            if (!string.IsNullOrEmpty(request.HotelGiataCode))
                parameters.Add("hotelGiataCode", request.HotelGiataCode);
            if (!string.IsNullOrEmpty(request.HotelAddress))
                parameters.Add("hotelAddress", request.HotelAddress);
            if (!string.IsNullOrEmpty(request.OriginTerminal))
                parameters.Add("originTerminal", request.OriginTerminal);
            if (!string.IsNullOrEmpty(request.DepartureIataCode))
                parameters.Add("departureIataCode", request.DepartureIataCode);
            if (!string.IsNullOrEmpty(request.InvoiceRef))
                parameters.Add("invoiceRef", request.InvoiceRef);
            if (!string.IsNullOrEmpty(request.YourRef))
                parameters.Add("yourRef", request.YourRef);

            if (request.ReturnTransfer)
            {
                if (!string.IsNullOrEmpty(request.ReturnAirlineCruiseline))
                    parameters.Add("returnAirlineCruiseline", request.ReturnAirlineCruiseline);
                if (!string.IsNullOrEmpty(request.ReturnFlightNumber))
                    parameters.Add("returnFlightNumber", request.ReturnFlightNumber);
                if (request.ReturnDepartureDate.HasValue)
                    parameters.Add("returnDepartureDate", request.ReturnDepartureDate.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(request.ReturnDepartureTime))
                    parameters.Add("returnDepartureTime", request.ReturnDepartureTime);
            }

            var response = await SendNonStaticRequestAsync("addTransferV2", parameters, cancellationToken);
            return ParseAddTransferResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transfer from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to add transfer", ex);
        }
    }

    public async Task<SunHotelsCancelTransferResult> CancelTransferAsync(string transferBookingId, string email, string language = "en", CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "transferBookingId", transferBookingId },
                { "email", email },
                { "language", language }
            };

            var response = await SendNonStaticRequestAsync("cancelTransferBooking", parameters, cancellationToken);
            return ParseCancelTransferResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to cancel transfer", ex);
        }
    }

    public async Task<List<SunHotelsBookingInfo>> GetTransferBookingInformationAsync(
        string? bookingId = null,
        DateTime? createdDateFrom = null,
        DateTime? createdDateTo = null,
        DateTime? arrivalDateFrom = null,
        DateTime? arrivalDateTo = null,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "language", language }
            };

            if (!string.IsNullOrEmpty(bookingId))
                parameters.Add("transferBookingId", bookingId);
            if (createdDateFrom.HasValue)
                parameters.Add("createdDateFrom", createdDateFrom.Value.ToString("yyyy-MM-dd"));
            if (createdDateTo.HasValue)
                parameters.Add("createdDateTo", createdDateTo.Value.ToString("yyyy-MM-dd"));
            if (arrivalDateFrom.HasValue)
                parameters.Add("arrivalDateFrom", arrivalDateFrom.Value.ToString("yyyy-MM-dd"));
            if (arrivalDateTo.HasValue)
                parameters.Add("arrivalDateTo", arrivalDateTo.Value.ToString("yyyy-MM-dd"));

            var response = await SendNonStaticRequestAsync("getTransferBookingInformation", parameters, cancellationToken);
            return ParseBookingInformation(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer booking information from SunHotels");
            throw new ExternalServiceException("SunHotels", "Failed to get transfer booking information", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<string> SendStaticRequestAsync(string method, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var url = $"{_staticApiUrl}/{method}";
        _logger.LogInformation("Sending static request to SunHotels: {Method} - URL: {Url}", method, url);

        // Username ve password ekle
        var allParameters = new Dictionary<string, string>(parameters)
        {
            { "userName", _config.Username },
            { "password", _config.Password }
        };

        // POST ile form-encoded data gönder
        var content = new FormUrlEncodedContent(allParameters);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SunHotels Static API error - Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);
            throw new ExternalServiceException("SunHotels", $"Static API returned {(int)response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // Error durumlarında log
        if (responseContent.Contains("<Error>"))
        {
            _logger.LogError("SunHotels Static API Error Response: {Response}", responseContent);
        }

        return responseContent;
    }

    private async Task<string> SendNonStaticRequestAsync(string method, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var url = $"{_nonStaticApiUrl}/{method}";
        _logger.LogInformation("Sending non-static request to SunHotels: {Method} - URL: {Url}", method, url);

        // Username ve password ekle
        var allParameters = new Dictionary<string, string>(parameters)
        {
            { "userName", _config.Username },
            { "password", _config.Password }
        };

        // POST ile form-encoded data gönder
        var content = new FormUrlEncodedContent(allParameters);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SunHotels NonStatic API error - Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);
            // SunHotels bazen 500 dönebiliyor; içeriği exception mesajına ekleyelim
            throw new ExternalServiceException("SunHotels", $"NonStatic API returned {(int)response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // ✅ İyileştirme: Daha fazla log detayı (debug için)
        _logger.LogInformation("SunHotels NonStatic API response length: {Length} characters", responseContent.Length);

        // Error durumlarında tam response'u logla
        if (responseContent.Contains("<Error>"))
        {
            _logger.LogError("SunHotels API Error Response (full): {Response}", responseContent);
        }
        else
        {
            // Başarılı yanıtlarda preview (ilk 3000 karakter)
            var preview = responseContent.Length > 3000 ? responseContent.Substring(0, 3000) + "..." : responseContent;
            _logger.LogDebug("SunHotels NonStatic API response preview: {Response}", preview);
        }

        return responseContent;
    }

    #endregion

    #region Parse Static Data Methods

    private List<SunHotelsDestination> ParseDestinations(string xmlResponse)
    {
        var destinations = new List<SunHotelsDestination>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";

            // ✅ Namespace fallback: önce namespace ile, yoksa namespace'siz dene
            var destinationElements = doc.Descendants(ns + "Destination");
            if (!destinationElements.Any())
            {
                destinationElements = doc.Descendants("Destination");
            }

            foreach (var elem in destinationElements)
            {
                // Helper: namespace veya namespace'siz element oku
                string GetValue(string elementName) =>
                    elem.Element(ns + elementName)?.Value ?? elem.Element(elementName)?.Value ?? "";

                destinations.Add(new SunHotelsDestination
                {
                    Id = GetValue("destination_id"),
                    Code = GetValue("DestinationCode"),
                    Name = GetValue("DestinationName"),
                    Country = GetValue("CountryName"),
                    CountryCode = GetValue("CountryCode"),
                    CountryId = GetValue("CountryId"),
                    TimeZone = GetValue("TimeZone")
                });
            }

            _logger.LogInformation("Parsed {Count} destinations from XML", destinations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing destinations XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return destinations;
    }

    private List<SunHotelsResort> ParseResorts(string xmlResponse)
    {
        var resorts = new List<SunHotelsResort>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var resortElements = doc.Descendants(ns + "Resort");

            foreach (var elem in resortElements)
            {
                resorts.Add(new SunHotelsResort
                {
                    Id = int.TryParse(elem.Element(ns + "ResortId")?.Value, out var id) ? id : 0,
                    Name = elem.Element(ns + "ResortName")?.Value ?? "",
                    DestinationId = elem.Element(ns + "destination_id")?.Value ?? "",
                    DestinationName = elem.Element(ns + "DestinationName")?.Value ?? "",
                    CountryName = elem.Element(ns + "CountryName")?.Value ?? "",
                    CountryCode = elem.Element(ns + "CountryCode")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} resorts from XML", resorts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing resorts XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return resorts;
    }

    private List<SunHotelsMeal> ParseMeals(string xmlResponse)
    {
        var meals = new List<SunHotelsMeal>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var mealElements = doc.Descendants(ns + "meal");

            foreach (var elem in mealElements)
            {
                var id = int.TryParse(elem.Element(ns + "id")?.Value, out var mealId) ? mealId : 0;
                var name = elem.Element(ns + "name")?.Value ?? "";

                // labels içindeki tüm label'ları topla
                var labels = elem.Descendants(ns + "label")
                    .Select(l => l.Element(ns + "text")?.Value)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                meals.Add(new SunHotelsMeal
                {
                    Id = id,
                    Name = name,
                    Labels = labels!
                });
            }

            _logger.LogInformation("Parsed {Count} meals from XML", meals.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing meals XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return meals;
    }
    private List<SunHotelsRoomType> ParseRoomTypes(string xmlResponse)
    {
        var roomTypes = new List<SunHotelsRoomType>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var roomTypeElements = doc.Descendants(ns + "roomType");

            foreach (var elem in roomTypeElements)
            {
                var id = int.TryParse(elem.Element(ns + "id")?.Value, out var roomTypeId) ? roomTypeId : 0;
                var name = elem.Element(ns + "name")?.Value ?? "";

                roomTypes.Add(new SunHotelsRoomType
                {
                    Id = id,
                    Name = name
                });
            }

            _logger.LogInformation("Parsed {Count} room types from XML", roomTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing room types XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return roomTypes;
    }

    private List<SunHotelsFeature> ParseFeatures(string xmlResponse)
    {
        var features = new List<SunHotelsFeature>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var featureElements = doc.Descendants(ns + "feature");

            foreach (var elem in featureElements)
            {
                var id = int.TryParse(elem.Attribute("id")?.Value, out var featureId) ? featureId : 0;
                var name = elem.Attribute("name")?.Value ?? "";

                features.Add(new SunHotelsFeature
                {
                    Id = id,
                    Name = name
                });
            }

            _logger.LogInformation("Parsed {Count} features from XML", features.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing features XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return features;
    }

    private List<SunHotelsLanguage> ParseLanguages(string xmlResponse)
    {
        var languages = new List<SunHotelsLanguage>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var languageElements = doc.Descendants(ns + "language");

            foreach (var elem in languageElements)
            {
                languages.Add(new SunHotelsLanguage
                {
                    Code = elem.Element(ns + "isoCode")?.Value ?? "",
                    Name = elem.Element(ns + "name")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} languages from XML", languages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing languages XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return languages;
    }

    private List<SunHotelsTheme> ParseThemes(string xmlResponse)
    {
        var themes = new List<SunHotelsTheme>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var themeElements = doc.Descendants(ns + "theme");

            foreach (var elem in themeElements)
            {
                themes.Add(new SunHotelsTheme
                {
                    Id = int.TryParse(elem.Attribute("id")?.Value, out var id) ? id : 0,
                    Name = elem.Attribute("name")?.Value ?? "",
                    EnglishName = elem.Attribute("name")?.Value ?? "" // Themes sadece name attribute'ı var
                });
            }

            _logger.LogInformation("Parsed {Count} themes from XML", themes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing themes XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return themes;
    }

    private List<SunHotelsTransferType> ParseTransferTypes(string xmlResponse)
    {
        var transferTypes = new List<SunHotelsTransferType>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var transferTypeElements = doc.Descendants(ns + "transferType");

            foreach (var elem in transferTypeElements)
            {
                transferTypes.Add(new SunHotelsTransferType
                {
                    Id = int.TryParse(elem.Element(ns + "id")?.Value, out var id) ? id : 0,
                    Name = elem.Element(ns + "name")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} transfer types from XML", transferTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing transfer types XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return transferTypes;
    }

    private List<SunHotelsNoteType> ParseNoteTypes(string xmlResponse)
    {
        var noteTypes = new List<SunHotelsNoteType>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";
            var noteTypeElements = doc.Descendants(ns + "noteType");

            foreach (var elem in noteTypeElements)
            {
                noteTypes.Add(new SunHotelsNoteType
                {
                    Id = int.TryParse(elem.Attribute("id")?.Value, out var id) ? id : 0,
                    Name = elem.Attribute("text")?.Value ?? ""
                });
            }

            _logger.LogInformation("Parsed {Count} note types from XML", noteTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing note types XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return noteTypes;
    }

    private List<SunHotelsStaticHotel> ParseStaticHotels(string xmlResponse)
    {
        var hotels = new List<SunHotelsStaticHotel>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";

            // Try with namespace first, fall back to no namespace
            var hotelElements = doc.Descendants(ns + "hotel");
            if (!hotelElements.Any())
            {
                hotelElements = doc.Descendants("hotel");
            }

            _logger.LogInformation("Found {Count} hotel elements in XML response", hotelElements.Count());

            foreach (var elem in hotelElements)
            {
                // Helper function to get element value with different possible names
                string GetElementValue(params string[] names)
                {
                    foreach (var name in names)
                    {
                        var value = elem.Element(ns + name)?.Value ?? elem.Element(name)?.Value;
                        if (!string.IsNullOrEmpty(value))
                            return value;
                    }
                    return "";
                }

                var hotel = new SunHotelsStaticHotel
                {
                    HotelId = int.TryParse(GetElementValue("hotel.id", "hotelId"), out var id) ? id : 0,
                    Name = GetElementValue("name", "hotelName"),
                    Description = GetElementValue("description"),
                    Address = GetElementValue("hotel.address", "address"),
                    ZipCode = GetElementValue("hotel.addr.zip", "zipCode"),
                    City = GetElementValue("hotel.addr.city", "city"),
                    Country = GetElementValue("hotel.addr.country", "countryName"),
                    CountryCode = GetElementValue("hotel.addr.countrycode", "countryCode"),
                    Category = int.TryParse(GetElementValue("classification", "category"), out var cat) ? cat : 0,
                    ResortId = int.TryParse(GetElementValue("resort_id", "resortId"), out var resortId) ? resortId : 0,
                    ResortName = GetElementValue("resort", "resortName"),
                    Phone = GetElementValue("phone"),
                    Fax = GetElementValue("fax"),
                    Email = GetElementValue("email"),
                    Website = GetElementValue("website")
                };

                // Parse coordinates
                var coordsElem = elem.Element(ns + "coordinates") ?? elem.Element("coordinates");
                if (coordsElem != null)
                {
                    if (double.TryParse(coordsElem.Element(ns + "latitude")?.Value ?? coordsElem.Element("latitude")?.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                        hotel.Latitude = lat;
                    if (double.TryParse(coordsElem.Element(ns + "longitude")?.Value ?? coordsElem.Element("longitude")?.Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                        hotel.Longitude = lng;
                }

                // Parse GIATA code from codes section
                var codesElem = elem.Element(ns + "codes") ?? elem.Element("codes");
                if (codesElem != null)
                {
                    var giataElem = codesElem.Elements().FirstOrDefault(e =>
                        e.Name.LocalName.ToLower().Contains("giata"));
                    if (giataElem != null)
                        hotel.GiataCode = giataElem.Value;
                }

                // Parse images
                int order = 0;
                var imagesContainer = elem.Element(ns + "images") ?? elem.Element("images");
                if (imagesContainer != null)
                {
                    foreach (var imgElem in imagesContainer.Elements())
                    {
                        if (imgElem.Name.LocalName == "image")
                        {
                            var fullSizeElem = imgElem.Element(ns + "fullSizeImage") ?? imgElem.Element("fullSizeImage");
                            var smallImageElem = imgElem.Element(ns + "smallImage") ?? imgElem.Element("smallImage");

                            var imageUrl = fullSizeElem?.Attribute("url")?.Value
                                        ?? smallImageElem?.Attribute("url")?.Value
                                        ?? imgElem.Element(ns + "url")?.Value
                                        ?? imgElem.Element("url")?.Value
                                        ?? imgElem.Value;

                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                // ✅ DÜZELTİLDİ: SunHotels resim URL formatı değiştirildi
                                // Eski format: http://xml.sunhotels.net/15/GetImage.aspx?id=12345 (500 hatası veriyor)
                                // Yeni format: https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id=12345&full=1 (çalışıyor)
                                if (imageUrl.StartsWith("?"))
                                {
                                    // Extract the ID from ?id=12345
                                    var imageId = imageUrl.TrimStart('?').Replace("id=", "");
                                    imageUrl = $"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={imageId}&full=1";
                                }
                                else if (imageUrl.Contains("GetImage.aspx"))
                                {
                                    // Convert old format to new format
                                    var idMatch = System.Text.RegularExpressions.Regex.Match(imageUrl, @"id=(\d+)");
                                    if (idMatch.Success)
                                    {
                                        var imageId = idMatch.Groups[1].Value;
                                        imageUrl = $"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={imageId}&full=1";
                                    }
                                }

                                hotel.Images.Add(new SunHotelsImage
                                {
                                    Url = imageUrl,
                                    Order = order++
                                });
                            }
                        }
                    }
                }

                // Parse features
                var featuresContainer = elem.Element(ns + "features") ?? elem.Element("features");
                if (featuresContainer != null)
                {
                    foreach (var featureElem in featuresContainer.Elements())
                    {
                        if (featureElem.Name.LocalName == "feature")
                        {
                            if (int.TryParse(featureElem.Attribute("id")?.Value, out var featureId))
                                hotel.FeatureIds.Add(featureId);
                        }
                    }
                }

                // Parse themes
                var themesContainer = elem.Element(ns + "themes") ?? elem.Element("themes");
                if (themesContainer != null)
                {
                    foreach (var themeElem in themesContainer.Elements())
                    {
                        if (themeElem.Name.LocalName == "theme")
                        {
                            if (int.TryParse(themeElem.Attribute("id")?.Value, out var themeId))
                                hotel.ThemeIds.Add(themeId);
                        }
                    }
                }

                // Parse rooms - structure is: roomtypes > roomtype > rooms > room
                var roomtypesContainer = elem.Element(ns + "roomtypes") ?? elem.Element("roomtypes");
                if (roomtypesContainer != null)
                {
                    foreach (var roomtypeElem in roomtypesContainer.Elements())
                    {
                        if (roomtypeElem.Name.LocalName == "roomtype")
                        {
                            var roomTypeName = roomtypeElem.Element(ns + "room.type")?.Value
                                            ?? roomtypeElem.Element("room.type")?.Value ?? "";
                            var roomTypeId = int.TryParse(
                                roomtypeElem.Element(ns + "roomtype.ID")?.Value
                                ?? roomtypeElem.Element("roomtype.ID")?.Value, out var rtId) ? rtId : 0;

                            var roomsContainer = roomtypeElem.Element(ns + "rooms") ?? roomtypeElem.Element("rooms");
                            if (roomsContainer != null)
                            {
                                foreach (var roomElem in roomsContainer.Elements())
                                {
                                    if (roomElem.Name.LocalName == "room")
                                    {
                                        var room = new SunHotelsStaticRoom
                                        {
                                            RoomTypeId = roomTypeId,
                                            Name = roomTypeName,
                                            EnglishName = roomTypeName,
                                            Description = roomtypeElem.Element(ns + "description")?.Value
                                                       ?? roomtypeElem.Element("description")?.Value,
                                            MaxOccupancy = int.TryParse(
                                                roomElem.Element(ns + "beds")?.Value
                                                ?? roomElem.Element("beds")?.Value, out var beds) ? beds : 0,
                                            MinOccupancy = 1
                                        };

                                        // Parse room features
                                        var roomFeaturesContainer = roomElem.Element(ns + "features") ?? roomElem.Element("features");
                                        if (roomFeaturesContainer != null)
                                        {
                                            foreach (var roomFeatureElem in roomFeaturesContainer.Elements())
                                            {
                                                if (roomFeatureElem.Name.LocalName == "feature")
                                                {
                                                    if (int.TryParse(roomFeatureElem.Attribute("id")?.Value, out var featureId))
                                                        room.FeatureIds.Add(featureId);
                                                }
                                            }
                                        }

                                        hotel.Rooms.Add(room);
                                    }
                                }
                            }
                        }
                    }
                }

                hotels.Add(hotel);
            }

            _logger.LogInformation("Parsed {Count} static hotels from XML with total {RoomCount} rooms",
                hotels.Count, hotels.Sum(h => h.Rooms.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing static hotels XML: {Xml}", xmlResponse?.Substring(0, Math.Min(500, xmlResponse?.Length ?? 0)));
        }

        return hotels;
    }

    #endregion
    #region Parse Search Results Methods

    private List<SunHotelsSearchResult> ParseSearchResults(string xmlResponse)
    {
        var results = new List<SunHotelsSearchResult>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var hotelElements = doc.Descendants("hotel");

            foreach (var elem in hotelElements)
            {
                var hotel = new SunHotelsSearchResult
                {
                    HotelId = elem.Element("hotelId")?.Value ?? "",
                    Name = elem.Element("hotelName")?.Value ?? "",
                    Description = elem.Element("description")?.Value ?? "",
                    Address = elem.Element("address")?.Value ?? "",
                    City = elem.Element("city")?.Value ?? "",
                    Country = elem.Element("countryName")?.Value ?? "",
                    Category = int.TryParse(elem.Element("category")?.Value, out var cat) ? cat : 0,
                    Latitude = double.TryParse(elem.Element("latitude")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ? lat : 0,
                    Longitude = double.TryParse(elem.Element("longitude")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var lng) ? lng : 0,
                    MinPrice = decimal.TryParse(elem.Element("minPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                    Currency = elem.Element("currency")?.Value ?? "EUR"
                };

                // Parse images
                int order = 0;
                foreach (var imgElem in elem.Descendants("image"))
                {
                    var imageUrl = imgElem.Element("url")?.Value ?? imgElem.Value;

                    // ✅ DÜZELTİLDİ: Resim URL formatını düzelt
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        if (imageUrl.StartsWith("?"))
                        {
                            var imageId = imageUrl.TrimStart('?').Replace("id=", "");
                            imageUrl = $"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={imageId}&full=1";
                        }
                        else if (imageUrl.Contains("GetImage.aspx"))
                        {
                            var idMatch = System.Text.RegularExpressions.Regex.Match(imageUrl, @"id=(\d+)");
                            if (idMatch.Success)
                            {
                                var imageId = idMatch.Groups[1].Value;
                                imageUrl = $"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={imageId}&full=1";
                            }
                        }

                        hotel.Images.Add(new SunHotelsImage
                        {
                            Url = imageUrl,
                            Order = order++
                        });
                    }
                }

                results.Add(hotel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing search results XML");
        }

        return results;
    }

    private List<SunHotelsSearchResultV3> ParseSearchResultsV3(string xmlResponse)
    {
        var results = new List<SunHotelsSearchResultV3>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);

            // Namespace'i al (varsa)
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // hotel elementlerini bul (namespace ile veya namespace'siz)
            var hotelElements = doc.Descendants(ns + "hotel");
            if (!hotelElements.Any())
            {
                hotelElements = doc.Descendants("hotel"); // Namespace olmadan dene
            }

            _logger.LogInformation("Found {Count} hotel elements in XML response", hotelElements.Count());

            foreach (var elem in hotelElements)
            {
                var hotel = new SunHotelsSearchResultV3
                {
                    HotelId = int.TryParse(elem.Element(ns + "hotel.id")?.Value, out var id) ? id : 0,
                    Name = "", // Statik datadan alınacak
                    Description = "",
                    Address = "",
                    City = "",
                    Country = "",
                    CountryCode = "",
                    Category = 0,
                    Latitude = 0,
                    Longitude = 0,
                    GiataCode = null,
                    ResortId = int.TryParse(elem.Element(ns + "resort_id")?.Value, out var resortId) ? resortId : 0,
                    ResortName = "",
                    MinPrice = 0, // Odalardan hesaplanacak
                    Currency = "EUR",
                    ReviewScore = null,
                    ReviewCount = null
                };

                // Parse roomtypes
                var roomtypesElem = elem.Element(ns + "roomtypes");
                if (roomtypesElem != null)
                {
                    foreach (var roomtypeElem in roomtypesElem.Elements(ns + "roomtype"))
                    {
                        var roomTypeId = int.TryParse(roomtypeElem.Element(ns + "roomtype.ID")?.Value, out var rtId) ? rtId : 0;

                        var roomsElem = roomtypeElem.Element(ns + "rooms");
                        if (roomsElem != null)
                        {
                            foreach (var roomElem in roomsElem.Elements(ns + "room"))
                            {
                                var roomId = int.TryParse(roomElem.Element(ns + "id")?.Value, out var rId) ? rId : 0;
                                var beds = int.TryParse(roomElem.Element(ns + "beds")?.Value, out var b) ? b : 0;
                                var extraBeds = int.TryParse(roomElem.Element(ns + "extrabeds")?.Value, out var eb) ? eb : 0;
                                var isSuperDeal = roomElem.Element(ns + "isSuperDeal")?.Value?.ToLower() == "true";
                                var isBestBuy = roomElem.Element(ns + "isBestBuy")?.Value?.ToLower() == "true";

                                // Parse meals - her meal için ayrı room olarak ekle
                                var mealsElem = roomElem.Element(ns + "meals");
                                if (mealsElem != null)
                                {
                                    foreach (var mealElem in mealsElem.Elements(ns + "meal"))
                                    {
                                        var mealId = int.TryParse(mealElem.Element(ns + "id")?.Value, out var mId) ? mId : 0;

                                        // Price parse et
                                        var priceElem = mealElem.Descendants(ns + "price").FirstOrDefault();
                                        var price = decimal.TryParse(priceElem?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0;
                                        var currency = priceElem?.Attribute("currency")?.Value ?? "EUR";

                                        // Discount varsa
                                        var discountElem = mealElem.Element(ns + "discount");
                                        decimal? discountAmount = null;
                                        if (discountElem != null && !discountElem.IsEmpty)
                                        {
                                            var amountElem = discountElem.Descendants(ns + "amount").FirstOrDefault();
                                            if (decimal.TryParse(amountElem?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var da))
                                                discountAmount = da;
                                        }

                                        var room = new SunHotelsRoomV3
                                        {
                                            RoomId = roomId,
                                            RoomTypeId = roomTypeId,
                                            RoomTypeName = "", // Statik datadan alınacak
                                            Name = "",
                                            Description = "",
                                            MealId = mealId,
                                            MealName = "", // Statik datadan alınacak
                                            Price = price,
                                            Currency = currency,
                                            IsRefundable = true,
                                            IsSuperDeal = isSuperDeal || isBestBuy,
                                            AvailableRooms = beds,
                                            OriginalPrice = discountAmount.HasValue ? price + discountAmount.Value : null
                                        };

                                        // Parse cancellation policies
                                        var policiesElem = roomElem.Element(ns + "cancellation_policies");
                                        if (policiesElem != null)
                                        {
                                            foreach (var policyElem in policiesElem.Elements(ns + "cancellation_policy"))
                                            {
                                                var deadline = int.TryParse(policyElem.Element(ns + "deadline")?.Value, out var dl) ? dl : 0;
                                                var percentage = decimal.TryParse(policyElem.Element(ns + "percentage")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct) ? pct : 0;

                                                // ✅ FIX: +22 gün buffer ekle (Python referans: freestays_deadline_hours = api_deadline_hours + (22 * 24))
                                                // SunHotels API'den gelen deadline (saat cinsinden) kullanıcıya fazla riskli
                                                // FreeStays olarak 22 gün ekstra süre veriyoruz
                                                var freestaysDeadlineHours = deadline + (22 * 24); // +528 saat (22 gün)

                                                room.CancellationPolicies.Add(new SunHotelsCancellationPolicy
                                                {
                                                    FromDate = DateTime.Now.AddHours(freestaysDeadlineHours),
                                                    Percentage = percentage,
                                                    FixedAmount = null,
                                                    NightsCharged = 0
                                                });
                                            }
                                        }

                                        hotel.Rooms.Add(room);
                                    }
                                }
                            }
                        }
                    }
                }

                // Parse feature IDs (eğer varsa)
                foreach (var featureElem in elem.Descendants(ns + "featureId"))
                {
                    if (int.TryParse(featureElem.Value, out var featureId))
                        hotel.FeatureIds.Add(featureId);
                }

                // Parse theme IDs (eğer varsa)
                foreach (var themeElem in elem.Descendants(ns + "themeId"))
                {
                    if (int.TryParse(themeElem.Value, out var themeId))
                        hotel.ThemeIds.Add(themeId);
                }

                // MinPrice'ı hesapla - tüm odaların en düşük fiyatı
                if (hotel.Rooms.Any())
                {
                    hotel.MinPrice = hotel.Rooms.Min(r => r.Price);
                }

                results.Add(hotel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing search results V3 XML");
        }

        return results;
    }

    private SunHotelsCancellationPolicy ParseCancellationPolicy(XElement elem)
    {
        return new SunHotelsCancellationPolicy
        {
            FromDate = DateTime.TryParse(elem.Element("deadline")?.Value, out var deadline) ? deadline : DateTime.MinValue,
            Percentage = decimal.TryParse(elem.Element("percentage")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct) ? pct : 0,
            FixedAmount = null,
            NightsCharged = 0
        };
    }

    #endregion

    #region Parse PreBook/Book Results Methods

    private SunHotelsPreBookResult ParsePreBookResult(string xmlResponse)
    {
        var result = new SunHotelsPreBookResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var preBookElem = doc.Descendants("preBookResult").FirstOrDefault() ?? doc.Root;

            if (preBookElem != null)
            {
                result.PreBookCode = preBookElem.Element("preBookCode")?.Value ?? "";
                result.TotalPrice = decimal.TryParse(preBookElem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
                result.Currency = preBookElem.Element("currency")?.Value ?? "EUR";
                result.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing pre-book result XML");
        }

        return result;
    }

    private SunHotelsPreBookResultV3 ParsePreBookResultV3(string xmlResponse)
    {
        var result = new SunHotelsPreBookResultV3();

        // Log raw XML for debugging - INFO level to see in production
        _logger.LogInformation("PreBookV3 Raw XML Response: {XmlResponse}", xmlResponse);

        try
        {
            var doc = XDocument.Parse(xmlResponse);

            // Log all element names for debugging
            var allElements = doc.Descendants().Select(e => e.Name.LocalName).Distinct().ToList();
            _logger.LogInformation("PreBookV3 XML Elements found: {Elements}", string.Join(", ", allElements));

            // SunHotels hata kontrolü - hem namespace'li hem namespace'siz
            var errorElem = doc.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("error", StringComparison.OrdinalIgnoreCase));
            if (errorElem != null)
            {
                var errorCode = errorElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("errorId", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? errorElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("code", StringComparison.OrdinalIgnoreCase))?.Value ?? "Unknown";
                var errorMessage = errorElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("errorMessage", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? errorElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("message", StringComparison.OrdinalIgnoreCase))?.Value ?? errorElem.Value;
                _logger.LogError("SunHotels PreBookV3 returned error - Code: {ErrorCode}, Message: {ErrorMessage}", errorCode, errorMessage);
                result.Error = errorMessage;
                result.ErrorCode = errorCode;
                return result;
            }

            // PreBookResult element'i bul - preBookResult veya preBookResultWithTaxAndFees olabilir
            var preBookElem = doc.Descendants().FirstOrDefault(e =>
                e.Name.LocalName.Equals("preBookResult", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("preBookResultWithTaxAndFees", StringComparison.OrdinalIgnoreCase))
                ?? doc.Root;

            _logger.LogInformation("PreBookV3 preBookElem found: {Found}, Name: {Name}",
                preBookElem != null, preBookElem?.Name.LocalName ?? "null");

            if (preBookElem != null)
            {
                // Namespace-agnostic, case-insensitive element lookup helper
                string GetElementValue(string localName) =>
                    preBookElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))?.Value;

                XElement GetElement(string localName) =>
                    preBookElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

                // PreBookCode - hem "preBookCode" hem "PreBookCode" olabilir
                result.PreBookCode = GetElementValue("PreBookCode") ?? "";

                // Price element'ini al - SunHotels V3'te fiyat CENT cinsinden geliyor
                // <Price currency="EUR">59430</Price> = 594.30 EUR
                var priceElem = GetElement("Price");
                if (priceElem != null)
                {
                    if (decimal.TryParse(priceElem.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceInCents))
                    {
                        result.TotalPrice = priceInCents / 100m; // Cent'ten EUR'a çevir
                    }
                    result.Currency = priceElem.Attribute("currency")?.Value ?? "EUR";
                }
                else
                {
                    // Fallback: totalPrice element'i dene
                    result.TotalPrice = decimal.TryParse(GetElementValue("totalPrice"), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
                    result.Currency = GetElementValue("currency") ?? "EUR";
                }

                result.NetPrice = decimal.TryParse(GetElementValue("netPrice"), NumberStyles.Any, CultureInfo.InvariantCulture, out var netPrice) ? netPrice : 0;

                // Tax element'i - xsi:nil="true" olabilir
                var taxElem = GetElement("Tax");
                if (taxElem != null && taxElem.Attribute(XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance") + "nil")?.Value != "true")
                {
                    result.TaxAmount = decimal.TryParse(taxElem.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var tax) ? tax / 100m : null;
                }

                result.FeeAmount = decimal.TryParse(GetElementValue("fee"), NumberStyles.Any, CultureInfo.InvariantCulture, out var fee) ? fee / 100m : null;

                _logger.LogInformation("PreBookV3 Parsed values - PreBookCode: {PreBookCode}, TotalPrice: {TotalPrice}, Currency: {Currency}",
                    result.PreBookCode, result.TotalPrice, result.Currency);
                result.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
                result.PriceChanged = GetElementValue("priceChanged")?.ToLower() == "true";
                result.OriginalPrice = decimal.TryParse(GetElementValue("originalPrice"), NumberStyles.Any, CultureInfo.InvariantCulture, out var origPrice) ? origPrice / 100m : null;

                // Notes parsing - hotel ve room notes
                var notes = preBookElem.Descendants().Where(e => e.Name.LocalName.Equals("Note", StringComparison.OrdinalIgnoreCase)).ToList();
                if (notes.Any())
                {
                    var allNoteTexts = notes.Select(n => n.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("text", StringComparison.OrdinalIgnoreCase))?.Value).Where(t => !string.IsNullOrEmpty(t));
                    result.HotelNotes = string.Join("\n", allNoteTexts);
                }

                if (DateTime.TryParse(GetElementValue("earliestNonFreeCancellationDateCET"), out var cancelDateCet))
                    result.EarliestNonFreeCancellationDateCET = cancelDateCet;
                if (DateTime.TryParse(GetElementValue("earliestNonFreeCancellationDateLocal"), out var cancelDateLocal))
                    result.EarliestNonFreeCancellationDateLocal = cancelDateLocal;

                // Parse price breakdown - namespace agnostic
                foreach (var breakdownElem in preBookElem.Descendants().Where(e => e.Name.LocalName.Equals("priceBreakdown", StringComparison.OrdinalIgnoreCase)))
                {
                    result.PriceBreakdown.Add(new SunHotelsPriceBreakdown
                    {
                        Date = DateTime.TryParse(breakdownElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("date", StringComparison.OrdinalIgnoreCase))?.Value, out var date) ? date : DateTime.MinValue,
                        Price = decimal.TryParse(breakdownElem.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("price", StringComparison.OrdinalIgnoreCase))?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var bPrice) ? bPrice / 100m : 0,
                        Currency = breakdownElem.Attribute("currency")?.Value ?? "EUR"
                    });
                }

                // Parse cancellation policies - namespace agnostic
                foreach (var policyElem in preBookElem.Descendants().Where(e => e.Name.LocalName.Equals("cancellationPolicy", StringComparison.OrdinalIgnoreCase)))
                {
                    result.CancellationPolicies.Add(ParseCancellationPolicy(policyElem));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing pre-book V3 result XML: {XmlResponse}", xmlResponse);
        }

        return result;
    }

    private SunHotelsBookResult ParseBookResult(string xmlResponse)
    {
        var result = new SunHotelsBookResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var bookElem = doc.Descendants("bookResult").FirstOrDefault() ?? doc.Root;

            if (bookElem != null)
            {
                result.Success = bookElem.Element("status")?.Value?.ToLower() == "success"
                              || !string.IsNullOrEmpty(bookElem.Element("bookingnumber")?.Value);
                result.BookingId = bookElem.Element("bookingnumber")?.Value ?? "";
                result.ConfirmationNumber = bookElem.Element("confirmationNumber")?.Value ?? "";
                result.Message = bookElem.Element("message")?.Value ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing book result XML");
        }

        return result;
    }

    private SunHotelsBookResultV3 ParseBookResultV3(string xmlResponse)
    {
        var result = new SunHotelsBookResultV3();

        // RAW XML'i logla
        _logger.LogWarning("=== SUNHOTELS BOOKV3 RAW XML RESPONSE ===");
        _logger.LogWarning("{RawXml}", xmlResponse);
        _logger.LogWarning("=== END SUNHOTELS BOOKV3 RAW XML ===");

        // Raw XML'i result'a kaydet
        result.RawXmlResponse = xmlResponse;

        try
        {
            var doc = XDocument.Parse(xmlResponse);

            // Namespace'i handle et
            XNamespace ns = "http://xml.sunhotels.net/15/";

            // ÖNCELİKLE Error element'ini kontrol et
            var errorElem = doc.Descendants(ns + "Error").FirstOrDefault()
                         ?? doc.Descendants("Error").FirstOrDefault();
            if (errorElem != null)
            {
                var errorType = errorElem.Element(ns + "ErrorType")?.Value
                             ?? errorElem.Element("ErrorType")?.Value ?? "Unknown";
                var errorMessage = errorElem.Element(ns + "Message")?.Value
                                ?? errorElem.Element("Message")?.Value ?? "Unknown error";

                _logger.LogError("SunHotels BookV3 returned error - Type: {ErrorType}, Message: {ErrorMessage}", errorType, errorMessage);

                result.Success = false;
                result.ErrorMessage = errorMessage;
                return result; // Hata durumunda booking bilgilerini parse etme
            }

            var bookResultElem = doc.Descendants(ns + "bookResult").FirstOrDefault()
                              ?? doc.Descendants("bookResult").FirstOrDefault();
            var bookingElem = bookResultElem?.Descendants(ns + "booking").FirstOrDefault()
                           ?? bookResultElem?.Descendants("booking").FirstOrDefault()
                           ?? doc.Descendants(ns + "booking").FirstOrDefault()
                           ?? doc.Descendants("booking").FirstOrDefault()
                           ?? doc.Root;

            if (bookingElem != null)
            {
                // Log all elements for debugging
                _logger.LogInformation("BookV3 Booking Element children: {Children}",
                    string.Join(", ", bookingElem.Elements().Select(e => e.Name.LocalName)));

                // SunHotels XML element isimleri (API dokümantasyonuna göre)
                result.BookingNumber = GetElementValue(bookingElem, ns, "bookingnumber");
                result.Success = !string.IsNullOrEmpty(result.BookingNumber);

                // hotel.id, hotel.name gibi noktalı isimler
                result.HotelId = int.TryParse(GetElementValue(bookingElem, ns, "hotel.id"), out var hotelId) ? hotelId : 0;
                result.HotelName = GetElementValue(bookingElem, ns, "hotel.name") ?? "";
                result.HotelAddress = GetElementValue(bookingElem, ns, "hotel.address") ?? "";
                result.HotelPhone = GetElementValue(bookingElem, ns, "hotel.phone");

                result.RoomType = GetElementValue(bookingElem, ns, "room.type") ?? "";
                result.EnglishRoomType = GetElementValue(bookingElem, ns, "room.englishType");
                result.NumberOfRooms = int.TryParse(GetElementValue(bookingElem, ns, "numberofrooms"), out var rooms) ? rooms : 0;

                result.MealId = int.TryParse(GetElementValue(bookingElem, ns, "mealId"), out var mealId) ? mealId : 0;
                result.MealName = GetElementValue(bookingElem, ns, "meal") ?? "";
                result.MealLabel = GetElementValue(bookingElem, ns, "mealLabel");
                result.EnglishMeal = GetElementValue(bookingElem, ns, "englishMeal");
                result.EnglishMealLabel = GetElementValue(bookingElem, ns, "englishMealLabel");

                result.CheckIn = DateTime.TryParse(GetElementValue(bookingElem, ns, "checkindate"), out var checkIn) ? checkIn : DateTime.MinValue;
                result.CheckOut = DateTime.TryParse(GetElementValue(bookingElem, ns, "checkoutdate"), out var checkOut) ? checkOut : DateTime.MinValue;

                result.Currency = GetElementValue(bookingElem, ns, "currency") ?? "EUR";
                result.BookingDate = DateTime.TryParse(GetElementValue(bookingElem, ns, "bookingdate"), out var bookingDate) ? bookingDate : DateTime.UtcNow;
                result.BookingDateTimezone = GetElementValue(bookingElem, ns, "bookingdate.timezone");

                result.Voucher = GetElementValue(bookingElem, ns, "voucher");
                result.YourRef = GetElementValue(bookingElem, ns, "yourref");
                result.InvoiceRef = GetElementValue(bookingElem, ns, "invoiceref");
                result.BookedBy = GetElementValue(bookingElem, ns, "bookedBy");

                result.ErrorMessage = GetElementValue(bookingElem, ns, "error");

                if (DateTime.TryParse(GetElementValue(bookingElem, ns, "earliestNonFreeCancellationDate.CET"), out var cancelDateCet))
                    result.EarliestNonFreeCancellationDateCET = cancelDateCet;
                if (DateTime.TryParse(GetElementValue(bookingElem, ns, "earliestNonFreeCancellationDate.Local"), out var cancelDateLocal))
                    result.EarliestNonFreeCancellationDateLocal = cancelDateLocal;

                // Parse prices
                var pricesElem = bookingElem.Descendants(ns + "prices").FirstOrDefault()
                              ?? bookingElem.Descendants("prices").FirstOrDefault();
                if (pricesElem != null)
                {
                    foreach (var priceElem in pricesElem.Elements())
                    {
                        if (decimal.TryParse(priceElem.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceVal))
                        {
                            result.Prices.Add(priceVal);
                        }
                    }
                    result.TotalPrice = result.Prices.Sum();
                }

                // Parse cancellation policies
                var cancellationPoliciesElems = bookingElem.Descendants(ns + "cancellationpolicies")
                    .Concat(bookingElem.Descendants("cancellationpolicies"));
                foreach (var policyElem in cancellationPoliciesElems)
                {
                    var text = GetElementValue(policyElem, ns, "text");
                    if (!string.IsNullOrEmpty(text))
                    {
                        result.CancellationPolicyTexts.Add(text);
                    }
                }

                _logger.LogInformation("BookV3 Parsed - BookingNumber={BookingNumber}, HotelId={HotelId}, HotelName={HotelName}, Success={Success}",
                    result.BookingNumber, result.HotelId, result.HotelName, result.Success);
            }
            else
            {
                _logger.LogWarning("BookV3 - No booking element found in XML response");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing book V3 result XML");
        }

        return result;
    }

    private string? GetElementValue(XElement parent, XNamespace ns, string localName)
    {
        return parent.Element(ns + localName)?.Value
            ?? parent.Element(localName)?.Value;
    }

    private SunHotelsCancelResult ParseCancelResult(string xmlResponse)
    {
        var result = new SunHotelsCancelResult();

        try
        {
            _logger.LogWarning("=== SUNHOTELS CANCEL RAW XML RESPONSE ===\n{Response}\n=== END CANCEL XML ===", xmlResponse);

            var doc = XDocument.Parse(xmlResponse);
            XNamespace ns = "http://xml.sunhotels.net/15/";

            // Root element'i bul (result)
            var resultElem = doc.Root;
            if (resultElem == null)
            {
                result.Message = "Empty response from SunHotels";
                return result;
            }

            // Code element'ini kontrol et
            var codeValue = resultElem.Element(ns + "Code")?.Value ?? resultElem.Element("Code")?.Value;
            if (int.TryParse(codeValue, out var code))
            {
                result.Code = code;
                // Code 0 = başarılı iptal
                result.Success = code == 0;
            }

            // CancellationPaymentMethod element'lerini parse et
            var paymentMethods = resultElem.Elements(ns + "CancellationPaymentMethod")
                .Concat(resultElem.Elements("CancellationPaymentMethod"));

            foreach (var pmElem in paymentMethods)
            {
                var pm = new CancellationPaymentMethod
                {
                    Id = int.TryParse(pmElem.Attribute("id")?.Value, out var pmId) ? pmId : 0,
                    Name = pmElem.Attribute("name")?.Value ?? ""
                };

                // Cancellation fee'leri parse et
                var feeElements = pmElem.Elements(ns + "cancellationfee")
                    .Concat(pmElem.Elements("cancellationfee"));

                foreach (var feeElem in feeElements)
                {
                    var fee = new CancellationFee
                    {
                        Currency = feeElem.Attribute("currency")?.Value ?? "EUR"
                    };

                    // Fee tutarı element içinde veya attribute olarak olabilir
                    var feeAmountStr = feeElem.Value ?? feeElem.Attribute("amount")?.Value;
                    if (decimal.TryParse(feeAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var feeAmount))
                    {
                        fee.Amount = feeAmount;
                    }

                    pm.CancellationFees.Add(fee);
                }

                // Cancellation info'ları parse et
                var cancellationElements = pmElem.Elements(ns + "cancellation")
                    .Concat(pmElem.Elements("cancellation"));

                foreach (var cancelElem in cancellationElements)
                {
                    var cancelInfo = new CancellationInfo
                    {
                        Type = cancelElem.Attribute("type")?.Value ?? "",
                        PolicyText = cancelElem.Descendants("text").FirstOrDefault()?.Value ?? ""
                    };

                    pm.Cancellations.Add(cancelInfo);
                }

                result.PaymentMethods.Add(pm);
            }

            // Eğer PaymentMethod varsa başarılı sayılır
            if (result.PaymentMethods.Count > 0)
            {
                result.Success = true;
                result.Message = "Booking cancelled successfully";
            }
            else if (result.Code != 0)
            {
                result.Success = false;
                result.Message = $"Cancellation failed with code: {result.Code}";
            }

            _logger.LogInformation("CancelBooking Parsed - Code={Code}, Success={Success}, PaymentMethods={Count}",
                result.Code, result.Success, result.PaymentMethods.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing cancel result XML");
            result.Message = $"Error parsing response: {ex.Message}";
        }

        return result;
    }

    #endregion

    #region Parse Other Results Methods

    private List<SunHotelsBookingInfo> ParseBookingInformation(string xmlResponse)
    {
        var bookings = new List<SunHotelsBookingInfo>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var bookingElements = doc.Descendants("booking");

            foreach (var elem in bookingElements)
            {
                var booking = new SunHotelsBookingInfo
                {
                    BookingNumber = elem.Element("bookingnumber")?.Value ?? "",
                    HotelId = int.TryParse(elem.Element("hotelId")?.Value, out var hotelId) ? hotelId : 0,
                    HotelName = elem.Element("hotelName")?.Value ?? "",
                    HotelAddress = elem.Element("hotelAddress")?.Value ?? "",
                    HotelPhone = elem.Element("hotelPhone")?.Value,
                    RoomType = elem.Element("roomType")?.Value ?? "",
                    NumberOfRooms = int.TryParse(elem.Element("numberOfRooms")?.Value, out var rooms) ? rooms : 0,
                    MealId = int.TryParse(elem.Element("mealId")?.Value, out var mealId) ? mealId : 0,
                    MealName = elem.Element("mealName")?.Value ?? "",
                    CheckIn = DateTime.TryParse(elem.Element("checkIn")?.Value, out var checkIn) ? checkIn : DateTime.MinValue,
                    CheckOut = DateTime.TryParse(elem.Element("checkOut")?.Value, out var checkOut) ? checkOut : DateTime.MinValue,
                    Status = elem.Element("status")?.Value ?? "",
                    TotalPrice = decimal.TryParse(elem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                    Currency = elem.Element("currency")?.Value ?? "EUR",
                    BookingDate = DateTime.TryParse(elem.Element("bookingDate")?.Value, out var bookingDate) ? bookingDate : DateTime.MinValue,
                    YourRef = elem.Element("yourRef")?.Value,
                    InvoiceRef = elem.Element("invoiceRef")?.Value,
                    Voucher = elem.Element("voucher")?.Value,
                    HasTransfer = elem.Element("hasTransfer")?.Value?.ToLower() == "true"
                };

                if (DateTime.TryParse(elem.Element("earliestNonFreeCancellationDateCET")?.Value, out var cancelDate))
                    booking.EarliestNonFreeCancellationDateCET = cancelDate;

                // Parse guests
                foreach (var guestElem in elem.Descendants("adultGuest"))
                {
                    booking.AdultGuests.Add(new SunHotelsGuest
                    {
                        FirstName = guestElem.Element("firstName")?.Value ?? "",
                        LastName = guestElem.Element("lastName")?.Value ?? ""
                    });
                }

                foreach (var childElem in elem.Descendants("childGuest"))
                {
                    booking.ChildrenGuests.Add(new SunHotelsChildGuest
                    {
                        FirstName = childElem.Element("firstName")?.Value ?? "",
                        LastName = childElem.Element("lastName")?.Value ?? "",
                        Age = int.TryParse(childElem.Element("age")?.Value, out var age) ? age : 0
                    });
                }

                // Parse cancellation policies
                foreach (var policyElem in elem.Descendants("cancellationPolicy"))
                {
                    booking.CancellationPolicies.Add(ParseCancellationPolicy(policyElem));
                }

                bookings.Add(booking);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing booking information XML");
        }

        return bookings;
    }

    private SunHotelsAmendmentPriceResult ParseAmendmentPriceResult(string xmlResponse)
    {
        var result = new SunHotelsAmendmentPriceResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var amendElem = doc.Descendants("amendmentPriceResult").FirstOrDefault() ?? doc.Root;

            if (amendElem != null)
            {
                result.AmendmentPossible = amendElem.Element("amendmentPossible")?.Value?.ToLower() == "true";
                result.Success = result.AmendmentPossible || amendElem.Element("error") == null;
                result.CurrentPrice = decimal.TryParse(amendElem.Element("currentPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var current) ? current : null;
                result.NewPrice = decimal.TryParse(amendElem.Element("newPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var newP) ? newP : null;
                result.PriceDifference = decimal.TryParse(amendElem.Element("priceDifference")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var diff) ? diff : null;
                result.Currency = amendElem.Element("currency")?.Value ?? "EUR";
                result.Message = amendElem.Element("message")?.Value ?? amendElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing amendment price result XML");
        }

        return result;
    }

    private SunHotelsAmendmentResult ParseAmendmentResult(string xmlResponse)
    {
        var result = new SunHotelsAmendmentResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var amendElem = doc.Descendants("amendmentResult").FirstOrDefault() ?? doc.Root;

            if (amendElem != null)
            {
                result.NewBookingNumber = amendElem.Element("newBookingnumber")?.Value;
                result.Success = !string.IsNullOrEmpty(result.NewBookingNumber);
                result.NewTotalPrice = decimal.TryParse(amendElem.Element("newTotalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : null;
                result.Currency = amendElem.Element("currency")?.Value ?? "EUR";
                result.Message = amendElem.Element("message")?.Value ?? amendElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing amendment result XML");
        }

        return result;
    }

    private SunHotelsSpecialRequestResult ParseSpecialRequestResult(string xmlResponse)
    {
        var result = new SunHotelsSpecialRequestResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var specialElem = doc.Descendants("specialRequestResult").FirstOrDefault() ?? doc.Root;

            if (specialElem != null)
            {
                result.Success = specialElem.Element("status")?.Value?.ToLower() == "success"
                              || specialElem.Element("error") == null;
                result.CurrentText = specialElem.Element("specialRequest")?.Value ?? specialElem.Element("text")?.Value;
                result.Message = specialElem.Element("message")?.Value ?? specialElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing special request result XML");
        }

        return result;
    }

    private List<SunHotelsTransferSearchResult> ParseTransferSearchResults(string xmlResponse)
    {
        var results = new List<SunHotelsTransferSearchResult>();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var transferElements = doc.Descendants("transfer");

            foreach (var elem in transferElements)
            {
                results.Add(new SunHotelsTransferSearchResult
                {
                    TransferId = int.TryParse(elem.Element("transferId")?.Value, out var id) ? id : 0,
                    TransferTypeId = int.TryParse(elem.Element("transferTypeId")?.Value, out var typeId) ? typeId : 0,
                    TransferTypeName = elem.Element("transferTypeName")?.Value ?? "",
                    PickupLocation = elem.Element("pickupLocation")?.Value ?? "",
                    DropoffLocation = elem.Element("dropoffLocation")?.Value ?? "",
                    MaxPassengers = int.TryParse(elem.Element("maxPassengers")?.Value, out var max) ? max : 0,
                    Price = decimal.TryParse(elem.Element("price")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                    Currency = elem.Element("currency")?.Value ?? "EUR",
                    IncludesReturn = elem.Element("includesReturn")?.Value?.ToLower() == "true",
                    VehicleType = elem.Element("vehicleType")?.Value,
                    Description = elem.Element("description")?.Value
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing transfer search results XML");
        }

        return results;
    }

    private SunHotelsAddTransferResult ParseAddTransferResult(string xmlResponse)
    {
        var result = new SunHotelsAddTransferResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var transferElem = doc.Descendants("addTransferResult").FirstOrDefault() ?? doc.Root;

            if (transferElem != null)
            {
                result.TransferBookingId = transferElem.Element("transferBookingId")?.Value;
                result.Success = !string.IsNullOrEmpty(result.TransferBookingId);
                result.ConfirmationNumber = transferElem.Element("confirmationNumber")?.Value;
                result.TotalPrice = decimal.TryParse(transferElem.Element("totalPrice")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
                result.Currency = transferElem.Element("currency")?.Value ?? "EUR";
                result.ErrorMessage = transferElem.Element("error")?.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing add transfer result XML");
        }

        return result;
    }

    private SunHotelsCancelTransferResult ParseCancelTransferResult(string xmlResponse)
    {
        var result = new SunHotelsCancelTransferResult();

        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var cancelElem = doc.Descendants("cancelTransferResult").FirstOrDefault() ?? doc.Root;

            if (cancelElem != null)
            {
                result.Success = cancelElem.Element("status")?.Value?.ToLower() == "success"
                              || cancelElem.Element("cancelled")?.Value?.ToLower() == "true";
                result.Message = cancelElem.Element("message")?.Value ?? "";
                if (decimal.TryParse(cancelElem.Element("refundAmount")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var refund))
                    result.RefundAmount = refund;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing cancel transfer result XML");
        }

        return result;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Arama sonuçlarını static cache verileriyle zenginleştirir (name, description, vs.)
    /// Batch query kullanarak N+1 problemini önler
    /// </summary>
    private async Task EnrichHotelsWithStaticDataAsync(List<SunHotelsSearchResultV3> hotels, string requestedLanguage, CancellationToken cancellationToken)
    {
        try
        {
            if (!hotels.Any())
            {
                _logger.LogInformation("EnrichHotelsWithStaticDataAsync - No hotels to enrich");
                return;
            }

            // ✅ Locale göz ardı: Her zaman İngilizce kullan (Python standardı)
            var language = "en";
            _logger.LogInformation("EnrichHotelsWithStaticDataAsync - Enriching {Count} hotels with language {Language}", hotels.Count, language);

            // Tüm gerekli ID'leri topla
            var hotelIds = hotels.Select(h => h.HotelId).Distinct().ToList();
            var resortIds = hotels.Where(h => h.ResortId > 0).Select(h => h.ResortId).Distinct().ToList();
            var mealIds = hotels.SelectMany(h => h.Rooms).Where(r => r.MealId > 0).Select(r => r.MealId).Distinct().ToList();
            var roomTypeIds = hotels.SelectMany(h => h.Rooms).Where(r => r.RoomTypeId > 0).Select(r => r.RoomTypeId).Distinct().ToList();

            _logger.LogInformation("EnrichHotelsWithStaticDataAsync - Need to fetch: {HotelCount} hotels, {ResortCount} resorts, {MealCount} meals, {RoomTypeCount} room types",
                hotelIds.Count, resortIds.Count, mealIds.Count, roomTypeIds.Count);
            _logger.LogInformation("EnrichHotelsWithStaticDataAsync - Hotel IDs: {HotelIds}", string.Join(", ", hotelIds.Take(10)));

            // Batch query ile tüm datayı al (tek seferde 4 sorgu)
            var hotelsDict = await _cacheService.GetHotelsByIdsAsync(hotelIds, language, cancellationToken);
            var resortsDict = await _cacheService.GetResortsByIdsAsync(resortIds, language, cancellationToken);
            var mealsDict = await _cacheService.GetMealsByIdsAsync(mealIds, language, cancellationToken);
            var roomTypesDict = await _cacheService.GetRoomTypesByIdsAsync(roomTypeIds, language, cancellationToken);

            _logger.LogInformation("EnrichHotelsWithStaticDataAsync - Fetched {HotelCount} hotels, {ResortCount} resorts, {MealCount} meals, {RoomTypeCount} room types from cache (language: {Language})",
                hotelsDict.Count, resortsDict.Count, mealsDict.Count, roomTypesDict.Count, language);

            // ✅ 3-KADEME FALLBACK (Python referansı): Cache → DB → API
            var missingHotelCount = hotelIds.Count - hotelsDict.Count;

            // KADEME 2: Cache'de olmayan oteller için DB'ye bak (Python'da yok ama stabilite için iyi)
            if (missingHotelCount > 0)
            {
                _logger.LogInformation("EnrichHotelsWithStaticDataAsync - {Count} hotels missing in cache, checking DB...",
                    missingHotelCount);

                try
                {
                    var missingHotelIds = hotelIds.Where(id => !hotelsDict.ContainsKey(id)).ToList();

                    // DB'den statik otelleri çek (SunHotelsHotels tablosu)
                    var dbHotels = await _cacheService.GetHotelsByIdsFromDbAsync(missingHotelIds, language, cancellationToken);

                    foreach (var dbHotel in dbHotels)
                    {
                        if (!hotelsDict.ContainsKey(dbHotel.Key))
                        {
                            hotelsDict[dbHotel.Key] = dbHotel.Value;
                        }
                    }

                    var dbFoundCount = dbHotels.Count;
                    missingHotelCount = hotelIds.Count - hotelsDict.Count;

                    _logger.LogInformation("EnrichHotelsWithStaticDataAsync - Found {Count} hotels in DB, {Remaining} still missing",
                        dbFoundCount, missingHotelCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DB fallback failed, will try API");
                }
            }

            // KADEME 3: Hala eksik oteller varsa REAL-TIME API çağrısı (Python: _enrich_hotels_with_static_data)
            if (missingHotelCount > 0)
            {
                _logger.LogWarning("EnrichHotelsWithStaticDataAsync - {Count} hotels missing in cache. Fetching from API (batch 50)...",
                    missingHotelCount);

                try
                {
                    var missingHotelIds = hotelIds.Where(id => !hotelsDict.ContainsKey(id)).ToList();
                    // Python referansı: _enrich_hotels_with_static_data() - GetStaticHotelsAndRooms batch (50'lik)
                    const int apiBatchSize = 50;
                    var apiFetchedCount = 0;

                    for (int i = 0; i < missingHotelIds.Count; i += apiBatchSize)
                    {
                        var batchIds = missingHotelIds.Skip(i).Take(apiBatchSize).ToList();
                        var hotelIdsParam = string.Join(",", batchIds);

                        _logger.LogInformation("Fetching batch {BatchNumber} from GetStaticHotelsAndRooms API ({Count} hotels)",
                            (i / apiBatchSize) + 1, batchIds.Count);

                        // GetStaticHotelsAndRooms API çağrısı (SunHotels static API) - her zaman "en"
                        var apiHotels = await GetStaticHotelsAndRoomsAsync(null, hotelIdsParam, null, "en", cancellationToken);

                        foreach (var apiHotel in apiHotels)
                        {
                            if (!hotelsDict.ContainsKey(apiHotel.HotelId))
                            {
                                // API'den gelen oteli geçici cache dictionary'e ekle
                                hotelsDict[apiHotel.HotelId] = new SunHotelsHotelCache
                                {
                                    HotelId = apiHotel.HotelId,
                                    Name = apiHotel.Name,
                                    Description = apiHotel.Description ?? "",
                                    Address = apiHotel.Address ?? "",
                                    City = apiHotel.City ?? "",
                                    Country = apiHotel.Country ?? "",
                                    CountryCode = apiHotel.CountryCode ?? "",
                                    Category = apiHotel.Category,
                                    Latitude = apiHotel.Latitude,
                                    Longitude = apiHotel.Longitude,
                                    Phone = apiHotel.Phone,
                                    Email = apiHotel.Email,
                                    Website = apiHotel.Website,
                                    GiataCode = apiHotel.GiataCode,
                                    ResortId = apiHotel.ResortId,
                                    ResortName = apiHotel.ResortName,
                                    ImageUrls = apiHotel.Images.Any()
                                        ? System.Text.Json.JsonSerializer.Serialize(apiHotel.Images.Select(x => x.Url).ToList())
                                        : "[]",
                                    FeatureIds = apiHotel.FeatureIds.Any()
                                        ? System.Text.Json.JsonSerializer.Serialize(apiHotel.FeatureIds)
                                        : "[]",
                                    ThemeIds = apiHotel.ThemeIds.Any()
                                        ? System.Text.Json.JsonSerializer.Serialize(apiHotel.ThemeIds)
                                        : "[]",
                                    Language = "en" // ✅ Sabit İngilizce
                                };
                                apiFetchedCount++;
                            }
                        }

                        // Rate limiting: 50 otel her seferde, biraz bekle
                        if (i + apiBatchSize < missingHotelIds.Count)
                        {
                            await Task.Delay(500); // 500ms throttle - no cancellation token
                        }
                    }

                    _logger.LogInformation("EnrichHotelsWithStaticDataAsync - Fetched {Count} hotels from real-time API", apiFetchedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching missing hotels from API. Continuing with cached data only.");
                    // API hatası olsa bile cache'deki verilerle devam et
                }
            }

            // Her otel için static datayı memory'den join et
            foreach (var hotel in hotels)
            {
                // Hotel bilgilerini cache'den al
                if (hotelsDict.TryGetValue(hotel.HotelId, out var hotelCache))
                {
                    hotel.Name = hotelCache.Name;
                    hotel.Description = hotelCache.Description ?? "";
                    hotel.Address = hotelCache.Address ?? "";
                    hotel.City = hotelCache.City ?? "";
                    hotel.Country = hotelCache.Country ?? "";
                    hotel.CountryCode = hotelCache.CountryCode ?? "";
                    hotel.Category = hotelCache.Category;
                    hotel.Latitude = hotelCache.Latitude ?? 0;
                    hotel.Longitude = hotelCache.Longitude ?? 0;
                    hotel.Phone = hotelCache.Phone;
                    hotel.Email = hotelCache.Email;
                    hotel.Website = hotelCache.Website;
                    hotel.GiataCode = hotelCache.GiataCode;

                    // Image URL'lerini parse et (JSON array'den)
                    if (!string.IsNullOrEmpty(hotelCache.ImageUrls) && hotelCache.ImageUrls != "[]")
                    {
                        try
                        {
                            var imageUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(hotelCache.ImageUrls);
                            if (imageUrls != null && imageUrls.Any())
                            {
                                hotel.ImageUrls.AddRange(imageUrls);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse ImageUrls JSON for hotel {HotelId}", hotel.HotelId);
                        }
                    }

                    // Feature ID'lerini parse et (JSON array'den)
                    if (!string.IsNullOrEmpty(hotelCache.FeatureIds) && hotelCache.FeatureIds != "[]")
                    {
                        try
                        {
                            var featureIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(hotelCache.FeatureIds);
                            if (featureIds != null && featureIds.Any())
                            {
                                hotel.FeatureIds.AddRange(featureIds);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse FeatureIds JSON for hotel {HotelId}", hotel.HotelId);
                        }
                    }

                    // Theme ID'lerini parse et (JSON array'den)
                    if (!string.IsNullOrEmpty(hotelCache.ThemeIds) && hotelCache.ThemeIds != "[]")
                    {
                        try
                        {
                            var themeIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(hotelCache.ThemeIds);
                            if (themeIds != null && themeIds.Any())
                            {
                                hotel.ThemeIds.AddRange(themeIds);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse ThemeIds JSON for hotel {HotelId}", hotel.HotelId);
                        }
                    }
                }

                // Resort bilgisini al
                if (hotel.ResortId > 0 && resortsDict.TryGetValue(hotel.ResortId, out var resort))
                {
                    hotel.ResortName = resort.Name;
                }

                // Her oda için meal ve room type bilgilerini al
                foreach (var room in hotel.Rooms)
                {
                    // Meal name
                    if (room.MealId > 0 && mealsDict.TryGetValue(room.MealId, out var meal))
                    {
                        room.MealName = meal.Name;
                    }

                    // Room type name
                    if (room.RoomTypeId > 0 && roomTypesDict.TryGetValue(room.RoomTypeId, out var roomType))
                    {
                        room.RoomTypeName = roomType.Name;
                        room.Name = roomType.Name; // Room name olarak da kullan
                    }
                }
            }

            _logger.LogInformation("EnrichHotelsWithStaticDataAsync - Enrichment completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching hotels with static data");
            // Hata durumunda devam et, static data olmadan da çalışabilir
        }
    }

    #endregion
}

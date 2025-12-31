using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

/// <summary>
/// Public API - Ziyaretçilere gösterilecek otel ve destinasyon endpoint'leri
/// </summary>
[ApiController]
[Route("api/v1/public")]
[Produces("application/json")]
[AllowAnonymous]
public class PublicHotelsController : ControllerBase
{
    private readonly ISunHotelsCacheService _cacheService;
    private readonly ILogger<PublicHotelsController> _logger;

    public PublicHotelsController(
        ISunHotelsCacheService cacheService,
        ILogger<PublicHotelsController> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Featured Hotels

    /// <summary>
    /// Yıldız sayısına göre popüler otelleri getirir (dil bazlı)
    /// </summary>
    /// <param name="stars">Otel yıldız sayısı (3, 4, 5 gibi). Varsayılan: tümü</param>
    /// <param name="count">Kaç otel getirileceği. Varsayılan: 10</param>
    /// <param name="acceptLanguage">Dil kodu (Accept-Language header'dan)</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("featured-hotels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedHotels(
        [FromQuery] int? stars = null,
        [FromQuery] int count = 10,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var language = ParseLanguage(acceptLanguage);
            _logger.LogInformation("Getting featured hotels with {Stars} stars, language: {Language}", stars, language);

            var allHotels = await _cacheService.GetAllHotelsAsync(language, cancellationToken);

            // Dil filtrelemesi
            var filteredByLanguage = allHotels
                .Where(h => h.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Yıldız filtrelemesi
            if (stars.HasValue)
            {
                filteredByLanguage = filteredByLanguage
                    .Where(h => h.Category == stars.Value)
                    .ToList();
            }

            // Popüler otelleri seç (en fazla resmi olanlar ve feature'ları çok olanlar)
            var featured = filteredByLanguage
                .Select(h => new
                {
                    Hotel = h,
                    ImageCount = ParseJsonArray(h.ImageUrls)?.Count ?? 0,
                    FeatureCount = ParseJsonArray(h.FeatureIds)?.Count ?? 0,
                    ThemeCount = ParseJsonArray(h.ThemeIds)?.Count ?? 0
                })
                .OrderByDescending(x => x.ImageCount + x.FeatureCount + x.ThemeCount)
                .Take(count)
                .Select(x => new
                {
                    id = x.Hotel.HotelId,
                    name = x.Hotel.Name,
                    description = x.Hotel.Description,
                    stars = x.Hotel.Category,
                    city = x.Hotel.City,
                    country = x.Hotel.Country,
                    countryCode = x.Hotel.CountryCode,
                    address = x.Hotel.Address,
                    resort = new
                    {
                        id = x.Hotel.ResortId,
                        name = x.Hotel.ResortName
                    },
                    location = x.Hotel.Latitude.HasValue && x.Hotel.Longitude.HasValue
                        ? new
                        {
                            latitude = x.Hotel.Latitude.Value,
                            longitude = x.Hotel.Longitude.Value
                        }
                        : null,
                    images = ParseJsonArray(x.Hotel.ImageUrls) ?? new List<string>(),
                    featureIds = ParseJsonArray(x.Hotel.FeatureIds) ?? new List<string>(),
                    themeIds = ParseJsonArray(x.Hotel.ThemeIds) ?? new List<string>(),
                    contact = new
                    {
                        phone = x.Hotel.Phone,
                        email = x.Hotel.Email,
                        website = x.Hotel.Website
                    }
                })
                .ToList();

            return Ok(new
            {
                language,
                stars,
                count = featured.Count,
                hotels = featured
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting featured hotels");
            return StatusCode(500, new { message = "Bir hata oluştu" });
        }
    }

    #endregion

    #region Popular Destinations

    /// <summary>
    /// Ülke bazlı popüler destinasyonları getirir (dil bazlı)
    /// </summary>
    /// <param name="country">Ülke kodu (ör: TR, US). Boş bırakılırsa tüm ülkeler</param>
    /// <param name="count">Kaç destinasyon getirileceği. Varsayılan: 10</param>
    /// <param name="acceptLanguage">Dil kodu (Accept-Language header'dan)</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("popular-destinations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopularDestinations(
        [FromQuery] string? country = null,
        [FromQuery] int count = 10,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var language = ParseLanguage(acceptLanguage);
            _logger.LogInformation("Getting popular destinations for country: {Country}, language: {Language}", country, language);

            var allDestinations = await _cacheService.GetAllDestinationsAsync(cancellationToken);
            var allHotels = await _cacheService.GetAllHotelsAsync(language, cancellationToken);

            // Dil filtrelemesi (hoteller üzerinden)
            var languageHotels = allHotels
                .Where(h => h.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Ülke filtrelemesi
            var filteredDestinations = allDestinations.AsEnumerable();
            if (!string.IsNullOrEmpty(country))
            {
                filteredDestinations = filteredDestinations
                    .Where(d => d.CountryCode.Equals(country, StringComparison.OrdinalIgnoreCase));
            }

            // Her destinasyondaki otel sayısını hesapla ve popülerlik sırasına koy
            var popularDestinations = filteredDestinations
                .Select(d => new
                {
                    Destination = d,
                    HotelCount = languageHotels.Count(h =>
                        h.City.Equals(d.Name, StringComparison.OrdinalIgnoreCase) ||
                        h.ResortName.Contains(d.Name, StringComparison.OrdinalIgnoreCase))
                })
                .Where(x => x.HotelCount > 0)
                .OrderByDescending(x => x.HotelCount)
                .Take(count)
                .Select(x => new
                {
                    id = x.Destination.DestinationId,
                    code = x.Destination.DestinationCode,
                    name = x.Destination.Name,
                    country = x.Destination.Country,
                    countryCode = x.Destination.CountryCode,
                    countryId = x.Destination.CountryId,
                    timeZone = x.Destination.TimeZone,
                    hotelCount = x.HotelCount
                })
                .ToList();

            return Ok(new
            {
                language,
                country,
                count = popularDestinations.Count,
                destinations = popularDestinations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular destinations");
            return StatusCode(500, new { message = "Bir hata oluştu" });
        }
    }

    #endregion

    #region Romantic Hotels

    /// <summary>
    /// Romantik turlar için otelleri getirir (dil bazlı, theme bazlı)
    /// </summary>
    /// <param name="count">Kaç otel getirileceği. Varsayılan: 10</param>
    /// <param name="acceptLanguage">Dil kodu (Accept-Language header'dan)</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("romantic-hotels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRomanticHotels(
        [FromQuery] int count = 10,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var language = ParseLanguage(acceptLanguage);
            _logger.LogInformation("Getting romantic hotels, language: {Language}", language);

            var allHotels = await _cacheService.GetAllHotelsAsync(language, cancellationToken);
            var allThemes = await _cacheService.GetAllThemesAsync(cancellationToken);

            // Romantik temalı tema ID'lerini bul
            var romanticThemeIds = allThemes
                .Where(t => t.Name.Contains("Romantic", StringComparison.OrdinalIgnoreCase) ||
                           t.Name.Contains("Romantik", StringComparison.OrdinalIgnoreCase) ||
                           t.Name.Contains("Honeymoon", StringComparison.OrdinalIgnoreCase) ||
                           t.Name.Contains("Balayı", StringComparison.OrdinalIgnoreCase) ||
                           t.EnglishName.Contains("Romantic", StringComparison.OrdinalIgnoreCase) ||
                           t.EnglishName.Contains("Honeymoon", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.ThemeId.ToString())
                .ToList();

            // Dil ve tema filtrelemesi
            var romanticHotels = allHotels
                .Where(h => h.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .Select(h => new
                {
                    Hotel = h,
                    ThemeIds = ParseJsonArray(h.ThemeIds) ?? new List<string>(),
                    ImageCount = ParseJsonArray(h.ImageUrls)?.Count ?? 0,
                    FeatureCount = ParseJsonArray(h.FeatureIds)?.Count ?? 0
                })
                .Where(x => x.ThemeIds.Any(tid => romanticThemeIds.Contains(tid)))
                .OrderByDescending(x => x.Hotel.Category) // Önce yıldız sayısına göre
                .ThenByDescending(x => x.ImageCount + x.FeatureCount) // Sonra zenginliğe göre
                .Take(count)
                .Select(x => new
                {
                    id = x.Hotel.HotelId,
                    name = x.Hotel.Name,
                    description = x.Hotel.Description,
                    stars = x.Hotel.Category,
                    city = x.Hotel.City,
                    country = x.Hotel.Country,
                    countryCode = x.Hotel.CountryCode,
                    address = x.Hotel.Address,
                    resort = new
                    {
                        id = x.Hotel.ResortId,
                        name = x.Hotel.ResortName
                    },
                    location = x.Hotel.Latitude.HasValue && x.Hotel.Longitude.HasValue
                        ? new
                        {
                            latitude = x.Hotel.Latitude.Value,
                            longitude = x.Hotel.Longitude.Value
                        }
                        : null,
                    images = ParseJsonArray(x.Hotel.ImageUrls) ?? new List<string>(),
                    featureIds = ParseJsonArray(x.Hotel.FeatureIds) ?? new List<string>(),
                    themeIds = x.ThemeIds,
                    contact = new
                    {
                        phone = x.Hotel.Phone,
                        email = x.Hotel.Email,
                        website = x.Hotel.Website
                    }
                })
                .ToList();

            return Ok(new
            {
                language,
                count = romanticHotels.Count,
                hotels = romanticHotels
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting romantic hotels");
            return StatusCode(500, new { message = "Bir hata oluştu" });
        }
    }

    #endregion

    #region Accommodation Types

    /// <summary>
    /// Themes ve features bazlı konaklama tiplerini getirir (dil bazlı)
    /// </summary>
    /// <param name="acceptLanguage">Dil kodu (Accept-Language header'dan)</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("accommodation-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccommodationTypes(
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var language = ParseLanguage(acceptLanguage);
            _logger.LogInformation("Getting accommodation types, language: {Language}", language);

            var allThemes = await _cacheService.GetAllThemesAsync(cancellationToken);
            var allFeatures = await _cacheService.GetAllFeaturesAsync(language, cancellationToken);
            var allHotels = await _cacheService.GetAllHotelsAsync(language, cancellationToken);

            // Dil bazlı features
            var languageFeatures = allFeatures
                .Where(f => f.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Dil bazlı hoteller
            var languageHotels = allHotels
                .Where(h => h.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Her tema için otel sayısı
            var themesWithHotelCount = allThemes
                .Select(t => new
                {
                    Theme = t,
                    HotelCount = languageHotels.Count(h =>
                    {
                        var hotelThemeIds = ParseJsonArray(h.ThemeIds) ?? new List<string>();
                        return hotelThemeIds.Contains(t.ThemeId.ToString());
                    })
                })
                .Where(x => x.HotelCount > 0)
                .Select(x => new
                {
                    id = x.Theme.ThemeId,
                    name = x.Theme.Name,
                    englishName = x.Theme.EnglishName,
                    type = "theme",
                    hotelCount = x.HotelCount
                })
                .ToList();

            // Her feature için otel sayısı
            var featuresWithHotelCount = languageFeatures
                .Select(f => new
                {
                    Feature = f,
                    HotelCount = languageHotels.Count(h =>
                    {
                        var hotelFeatureIds = ParseJsonArray(h.FeatureIds) ?? new List<string>();
                        return hotelFeatureIds.Contains(f.FeatureId.ToString());
                    })
                })
                .Where(x => x.HotelCount > 0)
                .Select(x => new
                {
                    id = x.Feature.FeatureId,
                    name = x.Feature.Name,
                    englishName = (string?)null, // Features don't have EnglishName
                    type = "feature",
                    hotelCount = x.HotelCount
                })
                .ToList();

            return Ok(new
            {
                language,
                themes = themesWithHotelCount.OrderByDescending(t => t.hotelCount).ToList(),
                features = featuresWithHotelCount.OrderByDescending(f => f.hotelCount).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accommodation types");
            return StatusCode(500, new { message = "Bir hata oluştu" });
        }
    }

    #endregion

    #region Helper Methods

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
    /// JSON array string'i List&lt;string&gt;'e parse eder
    /// </summary>
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

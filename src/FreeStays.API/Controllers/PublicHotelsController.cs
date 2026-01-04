using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.Features.Settings.Queries;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using MediatR;
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
    private readonly ISunHotelsService _sunHotelsService;
    private readonly IMediator _mediator;
    private readonly ILogger<PublicHotelsController> _logger;

    public PublicHotelsController(
        ISunHotelsCacheService cacheService,
        ISunHotelsService sunHotelsService,
        IMediator mediator,
        ILogger<PublicHotelsController> logger)
    {
        _cacheService = cacheService;
        _sunHotelsService = sunHotelsService;
        _mediator = mediator;
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
                    images = FixImageUrls(ParseJsonArray(x.Hotel.ImageUrls)),
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
                    images = FixImageUrls(ParseJsonArray(x.Hotel.ImageUrls)),
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

    #region Last Minute Deals

    /// <summary>
    /// Son dakika otel fırsatlarını getirir (b2c=1)
    /// </summary>
    /// <param name="acceptLanguage">Dil kodu (Accept-Language header'dan)</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("hotels/last-minute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLastMinuteDeals(
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var language = ParseLanguage(acceptLanguage);
            _logger.LogInformation("Getting last minute hotel deals, language: {Language}", language);

            // Get settings for last minute configuration
            var allSettings = await _mediator.Send(new GetSiteSettingsQuery("site"), cancellationToken);
            var settingsDict = allSettings.ToDictionary(s => s.Key, s => s.Value);

            var maxOffers = int.TryParse(settingsDict.GetValueOrDefault("lastMinuteCount", "6"), out var count) ? count : 6;

            // Get pricing settings
            var profitMarginStr = settingsDict.GetValueOrDefault("profitMargin", "0");
            var defaultVatRateStr = settingsDict.GetValueOrDefault("defaultVatRate", "0");
            var extraFeeStr = settingsDict.GetValueOrDefault("extraFee", "0");

            var profitMargin = decimal.TryParse(profitMarginStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pm) ? pm / 100 : 0m;
            var defaultVatRate = decimal.TryParse(defaultVatRateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vat) ? vat / 100 : 0m;
            var extraFee = decimal.TryParse(extraFeeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ef) ? ef : 0m;

            // Calculate dates - use settings or default to tomorrow/day after
            var customCheckIn = settingsDict.GetValueOrDefault("lastMinuteCheckIn", "");
            var customCheckOut = settingsDict.GetValueOrDefault("lastMinuteCheckOut", "");

            DateTime checkIn, checkOut;
            if (!string.IsNullOrEmpty(customCheckIn) && DateTime.TryParse(customCheckIn, out var customIn) &&
                !string.IsNullOrEmpty(customCheckOut) && DateTime.TryParse(customCheckOut, out var customOut))
            {
                checkIn = customIn;
                checkOut = customOut;
            }
            else
            {
                checkIn = DateTime.Now.AddDays(1);
                checkOut = DateTime.Now.AddDays(3);
            }

            // Get custom text settings
            var title = settingsDict.GetValueOrDefault("lastMinuteTitle", "Last Minute Offers");
            var subtitle = settingsDict.GetValueOrDefault("lastMinuteSubtitle", "Book now and save up to 30% on selected hotels");
            var badgeText = settingsDict.GetValueOrDefault("lastMinuteBadgeText", "Hot Deals");

            // Popular destinations for last minute deals
            var destinations = new List<(string Id, string Name)>
            {
                ("10188", "Amsterdam"),
                ("10049", "Barcelona"),
                ("122", "Amsterdam"),
                ("10025", "Vienna"),
                ("10264", "Paris")
            };

            var allHotels = new List<object>();

            // Try each destination to get last minute deals
            foreach (var dest in destinations)
            {
                if (allHotels.Count >= maxOffers)
                    break;

                var searchRequest = new SunHotelsSearchRequestV3
                {
                    DestinationId = dest.Id,
                    Destination = dest.Name,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    Adults = 2,
                    Children = 0,
                    NumberOfRooms = 1,
                    Currency = "EUR",
                    Language = language,
                    B2C = true, // Last minute availability
                    SortBy = "price",
                    SortOrder = "asc"
                };

                try
                {
                    var hotels = await _sunHotelsService.SearchHotelsV3Async(searchRequest, cancellationToken);

                    // Take top 3 from each destination
                    foreach (var hotel in hotels.Take(3))
                    {
                        // Calculate final price with profit margin and VAT
                        var basePrice = hotel.MinPrice == 0 ? 0m : hotel.MinPrice;
                        var profitAmount = basePrice * profitMargin;
                        var taxAmount = profitAmount * defaultVatRate;
                        var finalPrice = basePrice + profitAmount + taxAmount + extraFee;

                        allHotels.Add(new
                        {
                            id = hotel.HotelId,
                            name = hotel.Name,
                            description = hotel.Description,
                            stars = hotel.Category, // Category = yıldız sayısı
                            city = hotel.City,
                            country = hotel.Country,
                            address = hotel.Address,
                            location = new { latitude = hotel.Latitude, longitude = hotel.Longitude },
                            images = FixImageUrls(hotel.ImageUrls),
                            minPrice = finalPrice,
                            currency = hotel.Currency,
                            reviewScore = hotel.ReviewScore,
                            reviewCount = hotel.ReviewCount,
                            rooms = hotel.Rooms?.Select(r => new
                            {
                                id = r.RoomId,
                                name = r.Name,
                                roomType = r.RoomTypeName,
                                mealType = r.MealName,
                                price = r.Price,
                                availableRooms = r.AvailableRooms,
                                isRefundable = r.IsRefundable,
                                isSuperDeal = r.IsSuperDeal
                            }).ToList(),
                            lastMinuteCheckIn = checkIn.ToString("yyyy-MM-dd"),
                            lastMinuteCheckOut = checkOut.ToString("yyyy-MM-dd")
                        });

                        if (allHotels.Count >= maxOffers)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get last minute deals for {Destination}", dest.Name);
                    continue;
                }
            }

            // Sort by price
            var sortedHotels = allHotels
                .OrderBy(h => (decimal?)((dynamic)h).minPrice ?? decimal.MaxValue)
                .Take(maxOffers)
                .ToList();

            return Ok(new
            {
                hotels = sortedHotels,
                total = sortedHotels.Count,
                isLastMinute = true,
                checkIn = checkIn.ToString("yyyy-MM-dd"),
                checkOut = checkOut.ToString("yyyy-MM-dd"),
                title,
                subtitle,
                badgeText,
                language
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last minute deals");
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

    /// <summary>
    /// SunHotels resim URL'lerini düzgün formata dönüştürür
    /// http://xml.sunhotels.net/15/GetImage.aspx?id=78713614 
    /// → https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id=78713614&full=1
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

using System.Text.Json;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using Microsoft.Extensions.Logging;

namespace FreeStays.Infrastructure.Caching;

/// <summary>
/// SunHotels için özelleştirilmiş Redis cache servisi
/// Destinasyonlar, popüler oteller ve arama sonuçları için cache layer
/// </summary>
public class SunHotelsRedisCacheService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<SunHotelsRedisCacheService> _logger;

    // Cache key prefixleri
    private const string DESTINATION_PREFIX = "sunhotels:destinations";
    private const string RESORT_PREFIX = "sunhotels:resorts";
    private const string HOTEL_SEARCH_PREFIX = "sunhotels:search";
    private const string HOTEL_DETAILS_PREFIX = "sunhotels:hotel";
    private const string POPULAR_HOTELS_PREFIX = "sunhotels:popular";

    // Cache süreleri (TTL)
    private static readonly TimeSpan DestinationCacheDuration = TimeSpan.FromHours(24); // Destinasyonlar günde bir değişir
    private static readonly TimeSpan ResortCacheDuration = TimeSpan.FromHours(12); // Resort'lar 12 saatte bir
    private static readonly TimeSpan HotelSearchCacheDuration = TimeSpan.FromMinutes(30); // Arama sonuçları 30 dk
    private static readonly TimeSpan HotelDetailsCacheDuration = TimeSpan.FromHours(2); // Otel detayları 2 saat
    private static readonly TimeSpan PopularHotelsCacheDuration = TimeSpan.FromHours(6); // Popüler oteller 6 saat

    public SunHotelsRedisCacheService(
        ICacheService cacheService,
        ILogger<SunHotelsRedisCacheService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Destinations

    /// <summary>
    /// Tüm destinasyonları cache'den al
    /// </summary>
    public async Task<List<SunHotelsDestination>?> GetDestinationsAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        var key = $"{DESTINATION_PREFIX}:{language}";
        return await _cacheService.GetAsync<List<SunHotelsDestination>>(key, cancellationToken);
    }

    /// <summary>
    /// Tüm destinasyonları cache'e kaydet
    /// </summary>
    public async Task SetDestinationsAsync(List<SunHotelsDestination> destinations, string language = "en", CancellationToken cancellationToken = default)
    {
        var key = $"{DESTINATION_PREFIX}:{language}";
        await _cacheService.SetAsync(key, destinations, DestinationCacheDuration, cancellationToken);
        _logger.LogInformation("Cached {Count} destinations for language: {Language}", destinations.Count, language);
    }

    #endregion

    #region Resorts

    /// <summary>
    /// Resort'ları cache'den al (destinationId'ye göre)
    /// </summary>
    public async Task<List<SunHotelsResort>?> GetResortsAsync(string? destinationId = null, string language = "en", CancellationToken cancellationToken = default)
    {
        var key = string.IsNullOrEmpty(destinationId)
            ? $"{RESORT_PREFIX}:{language}:all"
            : $"{RESORT_PREFIX}:{language}:{destinationId}";

        return await _cacheService.GetAsync<List<SunHotelsResort>>(key, cancellationToken);
    }

    /// <summary>
    /// Resort'ları cache'e kaydet
    /// </summary>
    public async Task SetResortsAsync(List<SunHotelsResort> resorts, string? destinationId = null, string language = "en", CancellationToken cancellationToken = default)
    {
        var key = string.IsNullOrEmpty(destinationId)
            ? $"{RESORT_PREFIX}:{language}:all"
            : $"{RESORT_PREFIX}:{language}:{destinationId}";

        await _cacheService.SetAsync(key, resorts, ResortCacheDuration, cancellationToken);
        _logger.LogInformation("Cached {Count} resorts for destinationId: {DestinationId}, language: {Language}",
            resorts.Count, destinationId ?? "all", language);
    }

    #endregion

    #region Hotel Search

    /// <summary>
    /// Otel arama sonuçlarını cache'den al
    /// </summary>
    public async Task<List<SunHotelsSearchResultV3>?> GetHotelSearchAsync(
        string destinationId,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        int children,
        CancellationToken cancellationToken = default)
    {
        var key = BuildSearchCacheKey(destinationId, checkIn, checkOut, adults, children);
        var result = await _cacheService.GetAsync<List<SunHotelsSearchResultV3>>(key, cancellationToken);

        if (result != null)
        {
            _logger.LogInformation("Cache HIT for hotel search: {Key}", key);
        }
        else
        {
            _logger.LogInformation("Cache MISS for hotel search: {Key}", key);
        }

        return result;
    }

    /// <summary>
    /// Otel arama sonuçlarını cache'e kaydet
    /// </summary>
    public async Task SetHotelSearchAsync(
        List<SunHotelsSearchResultV3> results,
        string destinationId,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        int children,
        CancellationToken cancellationToken = default)
    {
        var key = BuildSearchCacheKey(destinationId, checkIn, checkOut, adults, children);
        await _cacheService.SetAsync(key, results, HotelSearchCacheDuration, cancellationToken);
        _logger.LogInformation("Cached {Count} hotel search results: {Key}", results.Count, key);
    }

    private string BuildSearchCacheKey(string destinationId, DateTime checkIn, DateTime checkOut, int adults, int children)
    {
        return $"{HOTEL_SEARCH_PREFIX}:{destinationId}:{checkIn:yyyyMMdd}:{checkOut:yyyyMMdd}:{adults}:{children}";
    }

    #endregion

    #region Hotel Details

    /// <summary>
    /// Otel detaylarını cache'den al
    /// </summary>
    public async Task<SunHotelsSearchResultV3?> GetHotelDetailsAsync(
        int hotelId,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        CancellationToken cancellationToken = default)
    {
        var key = $"{HOTEL_DETAILS_PREFIX}:{hotelId}:{checkIn:yyyyMMdd}:{checkOut:yyyyMMdd}:{adults}";
        var result = await _cacheService.GetAsync<SunHotelsSearchResultV3>(key, cancellationToken);

        if (result != null)
        {
            _logger.LogInformation("Cache HIT for hotel details: HotelId={HotelId}", hotelId);
        }

        return result;
    }

    /// <summary>
    /// Otel detaylarını cache'e kaydet
    /// </summary>
    public async Task SetHotelDetailsAsync(
        SunHotelsSearchResultV3 hotel,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        CancellationToken cancellationToken = default)
    {
        var key = $"{HOTEL_DETAILS_PREFIX}:{hotel.HotelId}:{checkIn:yyyyMMdd}:{checkOut:yyyyMMdd}:{adults}";
        await _cacheService.SetAsync(key, hotel, HotelDetailsCacheDuration, cancellationToken);
        _logger.LogInformation("Cached hotel details: HotelId={HotelId}", hotel.HotelId);
    }

    #endregion

    #region Popular Hotels

    /// <summary>
    /// Popüler otelleri cache'den al (destinationId ve yıldız sayısına göre)
    /// </summary>
    public async Task<List<SunHotelsSearchResultV3>?> GetPopularHotelsAsync(
        string? destinationId = null,
        int? stars = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"{POPULAR_HOTELS_PREFIX}:{destinationId ?? "all"}:{stars?.ToString() ?? "all"}";
        return await _cacheService.GetAsync<List<SunHotelsSearchResultV3>>(key, cancellationToken);
    }

    /// <summary>
    /// Popüler otelleri cache'e kaydet
    /// </summary>
    public async Task SetPopularHotelsAsync(
        List<SunHotelsSearchResultV3> hotels,
        string? destinationId = null,
        int? stars = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"{POPULAR_HOTELS_PREFIX}:{destinationId ?? "all"}:{stars?.ToString() ?? "all"}";
        await _cacheService.SetAsync(key, hotels, PopularHotelsCacheDuration, cancellationToken);
        _logger.LogInformation("Cached {Count} popular hotels for destinationId: {DestinationId}, stars: {Stars}",
            hotels.Count, destinationId ?? "all", stars?.ToString() ?? "all");
    }

    #endregion

    #region Cache Invalidation

    /// <summary>
    /// Belirli bir destinasyon için tüm search cache'ini temizle
    /// </summary>
    public async Task InvalidateDestinationSearchCacheAsync(string destinationId, CancellationToken cancellationToken = default)
    {
        // Bilinen cache pattern: sunhotels:search:destinationId:*
        // Not: Redis SCAN komutu (pattern-based deletion) henüz ICacheService'de implement edilmediği için
        // burada bir workaround kullanıyoruz - Log mesajı ile admin'i bilgilendiriyoruz
        _logger.LogInformation("⚠️ Cache invalidation for destinationId: {DestinationId} - Recommended: Run Redis FLUSHDB or use Redis CLI: SCAN 0 MATCH 'sunhotels:search:{DestinationId}:*' then DEL", destinationId);

        // TODO: ICacheService'e pattern-based deletion metodu ekle
        // Geçici çözüm: Admin panelden manuel clear yapılabilir
        await Task.CompletedTask;
    }

    /// <summary>
    /// Belirli bir otel için detail cache'ini temizle
    /// </summary>
    public async Task InvalidateHotelDetailsCacheAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        // TODO: Pattern-based deletion implementation
        _logger.LogInformation("⚠️ Cache invalidation for hotelId: {HotelId} - Pattern: 'sunhotels:hotel:{HotelId}:*'", hotelId);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tüm SunHotels Redis cache'ini temizle (admin kullanımı)
    /// UYARI: Production'da dikkatli kullan - arama performansı düşecektir
    /// </summary>
    public async Task ClearAllCacheAsync(CancellationToken cancellationToken = default)
    {
        // TODO: ICacheService'e batch delete metodu ekle
        _logger.LogWarning("⚠️ ADMIN ACTION: Full SunHotels cache clear requested. Patterns to delete:");
        _logger.LogWarning("  - {Pattern}", $"{DESTINATION_PREFIX}:*");
        _logger.LogWarning("  - {Pattern}", $"{RESORT_PREFIX}:*");
        _logger.LogWarning("  - {Pattern}", $"{HOTEL_SEARCH_PREFIX}:*");
        _logger.LogWarning("  - {Pattern}", $"{HOTEL_DETAILS_PREFIX}:*");
        _logger.LogWarning("  - {Pattern}", $"{POPULAR_HOTELS_PREFIX}:*");
        _logger.LogWarning("Recommendation: Use Redis CLI: EVAL \"return redis.call('del', unpack(redis.call('keys', ARGV[1])))\" 0 'sunhotels:*'");

        await Task.CompletedTask;
    }

    #endregion
}

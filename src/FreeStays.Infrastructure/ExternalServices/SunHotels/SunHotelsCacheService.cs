using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Entities.Cache;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FreeStays.Infrastructure.ExternalServices.SunHotels;

/// <summary>
/// SunHotels cache verilerine erişim sağlayan servis
/// In-memory cache ile veritabanı cache'ini birleştirir
/// </summary>
public class SunHotelsCacheService : ISunHotelsCacheService
{
    private readonly FreeStaysDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<SunHotelsCacheService> _logger;

    // Memory cache süreleri
    private static readonly TimeSpan ShortCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MediumCacheDuration = TimeSpan.FromHours(2);
    private static readonly TimeSpan LongCacheDuration = TimeSpan.FromHours(6);

    // Cache keys
    private const string DestinationsCacheKey = "sunhotels_destinations";
    private const string ResortsCacheKey = "sunhotels_resorts";
    private const string MealsCacheKey = "sunhotels_meals";
    private const string RoomTypesCacheKey = "sunhotels_roomtypes";
    private const string FeaturesCacheKey = "sunhotels_features";
    private const string ThemesCacheKey = "sunhotels_themes";
    private const string LanguagesCacheKey = "sunhotels_languages";
    private const string TransferTypesCacheKey = "sunhotels_transfertypes";
    private const string NoteTypesCacheKey = "sunhotels_notetypes";
    private const string HotelsCacheKey = "sunhotels_hotels";
    private const string RoomsCacheKey = "sunhotels_rooms";
    private const string StatisticsCacheKey = "sunhotels_statistics";

    public SunHotelsCacheService(
        FreeStaysDbContext context,
        IMemoryCache memoryCache,
        ILogger<SunHotelsCacheService> logger)
    {
        _context = context;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    #region Destinations

    public async Task<List<SunHotelsDestinationCache>> GetAllDestinationsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            DestinationsCacheKey,
            () => _context.SunHotelsDestinations.AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsDestinationCache?> GetDestinationByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var destinations = await GetAllDestinationsAsync(cancellationToken);
        return destinations.FirstOrDefault(d => d.DestinationId == externalId.ToString());
    }

    public async Task<List<SunHotelsDestinationCache>> SearchDestinationsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllDestinationsAsync(cancellationToken);

        var destinations = await GetAllDestinationsAsync(cancellationToken);
        var lowerSearch = searchTerm.ToLowerInvariant();

        return destinations
            .Where(d => d.Name.ToLowerInvariant().Contains(lowerSearch) ||
                       (d.Country?.ToLowerInvariant().Contains(lowerSearch) ?? false))
            .ToList();
    }

    #endregion

    #region Resorts

    public async Task<List<SunHotelsResortCache>> GetAllResortsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            ResortsCacheKey,
            () => _context.SunHotelsResorts.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsResortCache?> GetResortByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var resorts = await GetAllResortsAsync(cancellationToken);
        return resorts.FirstOrDefault(r => r.ResortId == externalId);
    }

    public async Task<List<SunHotelsResortCache>> GetResortsByDestinationAsync(int destinationId, CancellationToken cancellationToken = default)
    {
        var resorts = await GetAllResortsAsync(cancellationToken);
        return resorts.Where(r => r.DestinationId == destinationId.ToString()).ToList();
    }

    public async Task<List<SunHotelsResortCache>> SearchResortsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllResortsAsync(cancellationToken);

        var resorts = await GetAllResortsAsync(cancellationToken);
        var lowerSearch = searchTerm.ToLowerInvariant();

        return resorts
            .Where(r => r.Name.ToLowerInvariant().Contains(lowerSearch))
            .ToList();
    }

    #endregion

    #region Meals

    public async Task<List<SunHotelsMealCache>> GetAllMealsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            MealsCacheKey,
            () => _context.SunHotelsMeals.AsNoTracking().OrderBy(m => m.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsMealCache?> GetMealByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var meals = await GetAllMealsAsync(cancellationToken);
        return meals.FirstOrDefault(m => m.MealId == externalId);
    }

    #endregion

    #region Room Types

    public async Task<List<SunHotelsRoomTypeCache>> GetAllRoomTypesAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            RoomTypesCacheKey,
            () => _context.SunHotelsRoomTypes.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsRoomTypeCache?> GetRoomTypeByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var roomTypes = await GetAllRoomTypesAsync(cancellationToken);
        return roomTypes.FirstOrDefault(r => r.RoomTypeId == externalId);
    }

    #endregion

    #region Features

    public async Task<List<SunHotelsFeatureCache>> GetAllFeaturesAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            FeaturesCacheKey,
            () => _context.SunHotelsFeatures.AsNoTracking().OrderBy(f => f.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsFeatureCache?> GetFeatureByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var features = await GetAllFeaturesAsync(cancellationToken);
        return features.FirstOrDefault(f => f.FeatureId == externalId);
    }

    public async Task<List<SunHotelsFeatureCache>> GetFeaturesByTypeAsync(string featureType, CancellationToken cancellationToken = default)
    {
        var features = await GetAllFeaturesAsync(cancellationToken);
        // TODO: Category field will be added later based on API response
        return features.ToList();
    }

    #endregion

    #region Themes

    public async Task<List<SunHotelsThemeCache>> GetAllThemesAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            ThemesCacheKey,
            () => _context.SunHotelsThemes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsThemeCache?> GetThemeByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var themes = await GetAllThemesAsync(cancellationToken);
        return themes.FirstOrDefault(t => t.ThemeId == externalId);
    }

    #endregion

    #region Languages

    public async Task<List<SunHotelsLanguageCache>> GetAllLanguagesAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            LanguagesCacheKey,
            () => _context.SunHotelsLanguages.AsNoTracking().OrderBy(l => l.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsLanguageCache?> GetLanguageByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var languages = await GetAllLanguagesAsync(cancellationToken);
        return languages.FirstOrDefault(l => l.LanguageCode.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Transfer Types

    public async Task<List<SunHotelsTransferTypeCache>> GetAllTransferTypesAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            TransferTypesCacheKey,
            () => _context.SunHotelsTransferTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsTransferTypeCache?> GetTransferTypeByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var transferTypes = await GetAllTransferTypesAsync(cancellationToken);
        return transferTypes.FirstOrDefault(t => t.TransferTypeId == externalId);
    }

    #endregion

    #region Note Types

    public async Task<List<SunHotelsNoteTypeCache>> GetAllNoteTypesAsync(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            NoteTypesCacheKey,
            () => _context.SunHotelsNoteTypes.AsNoTracking().OrderBy(n => n.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsNoteTypeCache?> GetNoteTypeByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var noteTypes = await GetAllNoteTypesAsync(cancellationToken);
        return noteTypes.FirstOrDefault(n => n.NoteTypeId == externalId);
    }

    #endregion

    #region Hotels

    public async Task<List<SunHotelsHotelCache>> GetAllHotelsAsync(CancellationToken cancellationToken = default)
    {
        // Oteller için memory cache kullanmıyoruz çünkü çok fazla veri olabilir
        return await _context.SunHotelsHotels
            .AsNoTracking()
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SunHotelsHotelCache?> GetHotelByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        return await _context.SunHotelsHotels
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HotelId == externalId, cancellationToken);
    }

    public async Task<List<SunHotelsHotelCache>> GetHotelsByDestinationAsync(int destinationId, CancellationToken cancellationToken = default)
    {
        // Hotel entity'de DestinationId yok, sadece ResortId var
        // Bu metod şu an için boş liste döndürüyor
        return new List<SunHotelsHotelCache>();
    }

    public async Task<List<SunHotelsHotelCache>> GetHotelsByResortAsync(int resortId, CancellationToken cancellationToken = default)
    {
        return await _context.SunHotelsHotels
            .AsNoTracking()
            .Where(h => h.ResortId == resortId)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SunHotelsHotelCache>> SearchHotelsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<SunHotelsHotelCache>();

        var lowerSearch = searchTerm.ToLowerInvariant();

        return await _context.SunHotelsHotels
            .AsNoTracking()
            .Where(h => EF.Functions.ILike(h.Name, $"%{searchTerm}%") ||
                       (h.Address != null && EF.Functions.ILike(h.Address, $"%{searchTerm}%")))
            .OrderBy(h => h.Name)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<SunHotelsHotelCache> Hotels, int TotalCount)> GetHotelsPaginatedAsync(
        int page,
        int pageSize,
        int? destinationId = null,
        int? resortId = null,
        int? minStars = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SunHotelsHotels.AsNoTracking();

        // DestinationId alan\u0131 entity'de yok, atlaniyor
        // if (destinationId.HasValue)
        //     query = query.Where(h => h.DestinationId == destinationId.Value);

        if (resortId.HasValue)
            query = query.Where(h => h.ResortId == resortId.Value);

        if (minStars.HasValue)
            query = query.Where(h => h.Category >= minStars.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var hotels = await query
            .OrderBy(h => h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (hotels, totalCount);
    }

    #endregion

    #region Rooms

    public async Task<List<SunHotelsRoomCache>> GetAllRoomsAsync(CancellationToken cancellationToken = default)
    {
        // Odalar için memory cache kullanmıyoruz çünkü çok fazla veri olabilir
        return await _context.SunHotelsRooms
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SunHotelsRoomCache?> GetRoomByIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        return await _context.SunHotelsRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoomTypeId == externalId, cancellationToken);
    }

    public async Task<List<SunHotelsRoomCache>> GetRoomsByHotelAsync(int hotelId, CancellationToken cancellationToken = default)
    {
        return await _context.SunHotelsRooms
            .AsNoTracking()
            .Where(r => r.HotelId == hotelId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Statistics

    public async Task<SunHotelsCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _memoryCache.GetOrCreateAsync(StatisticsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ShortCacheDuration;

            var stats = new SunHotelsCacheStatistics
            {
                DestinationCount = await _context.SunHotelsDestinations.CountAsync(cancellationToken),
                ResortCount = await _context.SunHotelsResorts.CountAsync(cancellationToken),
                MealCount = await _context.SunHotelsMeals.CountAsync(cancellationToken),
                RoomTypeCount = await _context.SunHotelsRoomTypes.CountAsync(cancellationToken),
                FeatureCount = await _context.SunHotelsFeatures.CountAsync(cancellationToken),
                ThemeCount = await _context.SunHotelsThemes.CountAsync(cancellationToken),
                LanguageCount = await _context.SunHotelsLanguages.CountAsync(cancellationToken),
                TransferTypeCount = await _context.SunHotelsTransferTypes.CountAsync(cancellationToken),
                NoteTypeCount = await _context.SunHotelsNoteTypes.CountAsync(cancellationToken),
                HotelCount = await _context.SunHotelsHotels.CountAsync(cancellationToken),
                RoomCount = await _context.SunHotelsRooms.CountAsync(cancellationToken),
                LastSyncTime = await GetLastSyncTimeAsync(cancellationToken)
            };

            return stats;
        }) ?? new SunHotelsCacheStatistics();
    }

    public async Task<DateTime?> GetLastSyncTimeAsync(CancellationToken cancellationToken = default)
    {
        // En son güncellenen kaydın tarihini al
        var lastSync = await _context.SunHotelsDestinations
            .AsNoTracking()
            .OrderByDescending(d => d.LastSyncedAt)
            .Select(d => d.LastSyncedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return lastSync;
    }

    #endregion

    #region Helper Methods

    private async Task<List<T>> GetFromCacheOrDbAsync<T>(
        string cacheKey,
        Func<Task<List<T>>> dbQuery,
        TimeSpan cacheDuration)
    {
        return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = cacheDuration;

            try
            {
                return await dbQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data for cache key: {CacheKey}", cacheKey);
                return new List<T>();
            }
        }) ?? new List<T>();
    }

    /// <summary>
    /// Tüm in-memory cache'i temizle
    /// </summary>
    public void ClearMemoryCache()
    {
        var cacheKeys = new[]
        {
            DestinationsCacheKey, ResortsCacheKey, MealsCacheKey,
            RoomTypesCacheKey, FeaturesCacheKey, ThemesCacheKey,
            LanguagesCacheKey, TransferTypesCacheKey, NoteTypesCacheKey,
            HotelsCacheKey, RoomsCacheKey, StatisticsCacheKey
        };

        foreach (var key in cacheKeys)
        {
            _memoryCache.Remove(key);
        }

        _logger.LogInformation("SunHotels memory cache cleared");
    }

    #endregion
}

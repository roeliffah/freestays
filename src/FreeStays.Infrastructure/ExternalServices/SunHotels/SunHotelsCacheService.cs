using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.SunHotels;
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

    public async Task<List<SunHotelsResortCache>> GetAllResortsAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            $"{ResortsCacheKey}_{language}",
            () => _context.SunHotelsResorts.Where(r => r.Language == language).AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsResortCache?> GetResortByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        var resorts = await GetAllResortsAsync(language, cancellationToken);
        return resorts.FirstOrDefault(r => r.ResortId == externalId);
    }

    public async Task<List<SunHotelsResortCache>> GetResortsByDestinationAsync(int destinationId, string language = "en", CancellationToken cancellationToken = default)
    {
        var resorts = await GetAllResortsAsync(language, cancellationToken);
        return resorts.Where(r => r.DestinationId == destinationId.ToString()).ToList();
    }

    public async Task<List<SunHotelsResortCache>> SearchResortsAsync(string searchTerm, string language = "en", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllResortsAsync(language, cancellationToken);

        var resorts = await GetAllResortsAsync(language, cancellationToken);
        var lowerSearch = searchTerm.ToLowerInvariant();

        return resorts
            .Where(r => r.Name.ToLowerInvariant().Contains(lowerSearch))
            .ToList();
    }

    #endregion

    #region Meals

    public async Task<List<SunHotelsMealCache>> GetAllMealsAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            $"{MealsCacheKey}_{language}",
            () => _context.SunHotelsMeals.Where(m => m.Language == language).AsNoTracking().OrderBy(m => m.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsMealCache?> GetMealByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        var meals = await GetAllMealsAsync(language, cancellationToken);
        return meals.FirstOrDefault(m => m.MealId == externalId);
    }

    #endregion

    #region Room Types

    public async Task<List<SunHotelsRoomTypeCache>> GetAllRoomTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            $"{RoomTypesCacheKey}_{language}",
            () => _context.SunHotelsRoomTypes.Where(r => r.Language == language).AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsRoomTypeCache?> GetRoomTypeByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        var roomTypes = await GetAllRoomTypesAsync(language, cancellationToken);
        return roomTypes.FirstOrDefault(r => r.RoomTypeId == externalId);
    }

    #endregion

    #region Features

    public async Task<List<SunHotelsFeatureCache>> GetAllFeaturesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            FeaturesCacheKey,
            () => _context.SunHotelsFeatures.Where(f => f.Language == language).AsNoTracking().OrderBy(f => f.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsFeatureCache?> GetFeatureByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        var features = await GetAllFeaturesAsync(language, cancellationToken);
        return features.FirstOrDefault(f => f.FeatureId == externalId);
    }

    public async Task<List<SunHotelsFeatureCache>> GetFeaturesByTypeAsync(string featureType, string language = "en", CancellationToken cancellationToken = default)
    {
        var features = await GetAllFeaturesAsync(language, cancellationToken);
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

    public async Task<List<SunHotelsTransferTypeCache>> GetAllTransferTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            $"{TransferTypesCacheKey}_{language}",
            () => _context.SunHotelsTransferTypes.Where(t => t.Language == language).AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsTransferTypeCache?> GetTransferTypeByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        var transferTypes = await GetAllTransferTypesAsync(language, cancellationToken);
        return transferTypes.FirstOrDefault(t => t.TransferTypeId == externalId);
    }

    #endregion

    #region Note Types

    public async Task<List<SunHotelsNoteTypeCache>> GetAllNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrDbAsync(
            $"{NoteTypesCacheKey}_{language}",
            () => _context.SunHotelsNoteTypes.Where(n => n.Language == language).AsNoTracking().OrderBy(n => n.Name).ToListAsync(cancellationToken),
            LongCacheDuration);
    }

    public async Task<SunHotelsNoteTypeCache?> GetNoteTypeByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        var noteTypes = await GetAllNoteTypesAsync(language, cancellationToken);
        return noteTypes.FirstOrDefault(n => n.NoteTypeId == externalId);
    }

    #endregion

    #region Hotels

    public async Task<List<SunHotelsHotelCache>> GetAllHotelsAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        // Oteller için memory cache kullanmıyoruz çünkü çok fazla veri olabilir
        return await _context.SunHotelsHotels
            .Where(h => h.Language == language)
            .AsNoTracking()
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SunHotelsHotelCache?> GetHotelByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetHotelByIdAsync - Looking for hotel {HotelId} with language {Language}", externalId, language);

        var result = await _context.SunHotelsHotels
            .Where(h => h.Language == language)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HotelId == externalId, cancellationToken);

        _logger.LogInformation("GetHotelByIdAsync - Hotel {HotelId} found: {Found}", externalId, result != null);

        return result;
    }

    public async Task<Dictionary<int, SunHotelsHotelCache>> GetHotelsByIdsAsync(IEnumerable<int> hotelIds, string language = "en", CancellationToken cancellationToken = default)
    {
        var ids = hotelIds.ToList();
        if (!ids.Any()) return new Dictionary<int, SunHotelsHotelCache>();

        _logger.LogInformation("GetHotelsByIdsAsync - Looking for {Count} hotels with language {Language}, IDs: {Ids}",
            ids.Count, language, string.Join(", ", ids.Take(5)));

        var hotels = await _context.SunHotelsHotels
            .Where(h => h.Language == language && ids.Contains(h.HotelId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("GetHotelsByIdsAsync - Found {Count} hotels in database", hotels.Count);

        return hotels.ToDictionary(h => h.HotelId, h => h);
    }

    /// <summary>
    /// DB'den direkt otelleri çek (cache bypass) - 3-kademe fallback için (Python referansı)
    /// Cache'de olmayan oteller için kullanılır, API çağrısı yapmadan önce DB'ye bakar
    /// </summary>
    public async Task<Dictionary<int, SunHotelsHotelCache>> GetHotelsByIdsFromDbAsync(IEnumerable<int> hotelIds, string language = "en", CancellationToken cancellationToken = default)
    {
        var ids = hotelIds.ToList();
        if (!ids.Any()) return new Dictionary<int, SunHotelsHotelCache>();

        _logger.LogInformation("GetHotelsByIdsFromDbAsync - Fetching {Count} hotels directly from DB (bypass cache), language: {Language}",
            ids.Count, language);

        var hotels = await _context.SunHotelsHotels
            .Where(h => h.Language == language && ids.Contains(h.HotelId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("GetHotelsByIdsFromDbAsync - Found {Count}/{Total} hotels in database", hotels.Count, ids.Count);

        return hotels.ToDictionary(h => h.HotelId, h => h);
    }

    public async Task<Dictionary<int, SunHotelsResortCache>> GetResortsByIdsAsync(IEnumerable<int> resortIds, string language = "en", CancellationToken cancellationToken = default)
    {
        var ids = resortIds.ToList();
        if (!ids.Any()) return new Dictionary<int, SunHotelsResortCache>();

        var resorts = await _context.SunHotelsResorts
            .Where(r => r.Language == language && ids.Contains(r.ResortId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return resorts.ToDictionary(r => r.ResortId, r => r);
    }

    public async Task<Dictionary<int, SunHotelsMealCache>> GetMealsByIdsAsync(IEnumerable<int> mealIds, string language = "en", CancellationToken cancellationToken = default)
    {
        var ids = mealIds.ToList();
        if (!ids.Any()) return new Dictionary<int, SunHotelsMealCache>();

        var meals = await _context.SunHotelsMeals
            .Where(m => m.Language == language && ids.Contains(m.MealId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return meals.ToDictionary(m => m.MealId, m => m);
    }

    public async Task<Dictionary<int, SunHotelsRoomTypeCache>> GetRoomTypesByIdsAsync(IEnumerable<int> roomTypeIds, string language = "en", CancellationToken cancellationToken = default)
    {
        var ids = roomTypeIds.ToList();
        if (!ids.Any()) return new Dictionary<int, SunHotelsRoomTypeCache>();

        var roomTypes = await _context.SunHotelsRoomTypes
            .Where(rt => rt.Language == language && ids.Contains(rt.RoomTypeId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return roomTypes.ToDictionary(rt => rt.RoomTypeId, rt => rt);
    }

    public async Task<List<SunHotelsHotelCache>> GetHotelsByDestinationAsync(int destinationId, string language = "en", CancellationToken cancellationToken = default)
    {
        // Hotel entity'de DestinationId yok, sadece ResortId var
        // Bu metod şu an için boş liste döndürüyor
        return new List<SunHotelsHotelCache>();
    }

    public async Task<List<SunHotelsHotelCache>> GetHotelsByResortAsync(int resortId, string language = "en", CancellationToken cancellationToken = default)
    {
        return await _context.SunHotelsHotels
            .Where(h => h.Language == language)
            .AsNoTracking()
            .Where(h => h.ResortId == resortId)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SunHotelsHotelCache>> SearchHotelsAsync(string searchTerm, string language = "en", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<SunHotelsHotelCache>();

        var lowerSearch = searchTerm.ToLowerInvariant();

        return await _context.SunHotelsHotels
            .Where(h => h.Language == language)
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
        string language = "en",
        int? destinationId = null,
        int? resortId = null,
        int? minStars = null,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.SunHotelsHotels.Where(h => h.Language == language).AsNoTracking();

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

    /// <summary>
    /// Gelişmiş otel arama - Cache tablolardan statik arama
    /// Tema, konum, ülke, yemek türü, özellik gibi kriterlere göre filtreler
    /// </summary>
    public async Task<HotelSearchResponse> SearchHotelsAdvancedAsync(
        HotelSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SunHotelsHotels.AsNoTracking();

        // Dil filtresi
        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            query = query.Where(h => h.Language == request.Language);
        }

        // Destinasyon filtresi
        if (request.DestinationIds != null && request.DestinationIds.Any())
        {
            // Resort üzerinden destination bağlantısı yapılacak
            var resortIds = await _context.SunHotelsResorts
                .Where(r => request.DestinationIds.Contains(r.DestinationId))
                .Select(r => r.ResortId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (resortIds.Any())
            {
                query = query.Where(h => resortIds.Contains(h.ResortId));
            }
        }

        // Resort filtresi
        if (request.ResortIds != null && request.ResortIds.Any())
        {
            query = query.Where(h => request.ResortIds.Contains(h.ResortId));
        }

        // Ülke kodu filtresi
        if (request.CountryCodes != null && request.CountryCodes.Any())
        {
            query = query.Where(h => request.CountryCodes.Contains(h.CountryCode));
        }

        // Ülke ismi filtresi
        if (request.CountryNames != null && request.CountryNames.Any())
        {
            query = query.Where(h => request.CountryNames.Contains(h.Country));
        }

        // Yıldız filtresi
        if (request.MinStars.HasValue)
        {
            query = query.Where(h => h.Category >= request.MinStars.Value);
        }

        if (request.MaxStars.HasValue)
        {
            query = query.Where(h => h.Category <= request.MaxStars.Value);
        }

        // Tema filtresi (JSONB array içinde arama)
        if (request.ThemeIds != null && request.ThemeIds.Any())
        {
            foreach (var themeId in request.ThemeIds)
            {
                // PostgreSQL JSONB array contains operatörü (@>)
                query = query.Where(h => EF.Functions.JsonContains(h.ThemeIds, $"[{themeId}]"));
            }
        }

        // Özellik filtresi (JSONB array içinde arama)
        if (request.FeatureIds != null && request.FeatureIds.Any())
        {
            foreach (var featureId in request.FeatureIds)
            {
                // PostgreSQL JSONB array contains operatörü (@>)
                query = query.Where(h => EF.Functions.JsonContains(h.FeatureIds, $"[{featureId}]"));
            }
        }

        // Serbest metin arama
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(h =>
                EF.Functions.ILike(h.Name, $"%{request.SearchTerm}%") ||
                EF.Functions.ILike(h.City, $"%{request.SearchTerm}%") ||
                (h.Address != null && EF.Functions.ILike(h.Address, $"%{request.SearchTerm}%")));
        }

        // Toplam kayıt sayısı
        var totalCount = await query.CountAsync(cancellationToken);

        // Sayfalama parametrelerini validate et
        var page = Math.Max(1, request.Page); // Minimum 1
        var pageSize = Math.Clamp(request.PageSize, 1, 100); // 1-100 arası

        // Sayfalama
        var hotels = await query
            .OrderBy(h => h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Response oluştur
        var response = new HotelSearchResponse
        {
            Hotels = hotels.Select(HotelSearchResultDto.FromCache).ToList(),
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            CurrentPage = page,
            PageSize = pageSize,
            SearchType = "static"
        };

        return response;
    }

    #endregion

    #region Rooms

    public async Task<List<SunHotelsRoomCache>> GetAllRoomsAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        // Odalar için memory cache kullanmıyoruz çünkü çok fazla veri olabilir
        return await _context.SunHotelsRooms
            .Where(r => r.Language == language)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SunHotelsRoomCache?> GetRoomByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default)
    {
        return await _context.SunHotelsRooms
            .Where(r => r.Language == language)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoomTypeId == externalId, cancellationToken);
    }

    public async Task<List<SunHotelsRoomCache>> GetRoomsByHotelAsync(int hotelId, string language = "en", CancellationToken cancellationToken = default)
    {
        return await _context.SunHotelsRooms
            .Where(r => r.Language == language)
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
                // Unique ID bazında say (her dil için ayrı kayıt olduğundan distinct yapıyoruz)
                DestinationCount = await _context.SunHotelsDestinations
                    .Select(d => d.DestinationId)
                    .Distinct()
                    .CountAsync(cancellationToken),

                ResortCount = await _context.SunHotelsResorts
                    .Select(r => r.ResortId)
                    .Distinct()
                    .CountAsync(cancellationToken),

                MealCount = await _context.SunHotelsMeals
                    .Select(m => m.MealId)
                    .Distinct()
                    .CountAsync(cancellationToken),

                RoomTypeCount = await _context.SunHotelsRoomTypes
                    .Select(r => r.RoomTypeId)
                    .Distinct()
                    .CountAsync(cancellationToken),

                FeatureCount = await _context.SunHotelsFeatures
                    .Select(f => f.FeatureId)
                    .Distinct()
                    .CountAsync(cancellationToken),

                ThemeCount = await _context.SunHotelsThemes
                    .Select(t => t.ThemeId)
                    .Distinct()
                    .CountAsync(cancellationToken),

                LanguageCount = await _context.SunHotelsLanguages
                    .Select(l => l.LanguageCode)
                    .Distinct()
                    .CountAsync(cancellationToken),

                TransferTypeCount = await _context.SunHotelsTransferTypes
                    .Select(t => t.TransferTypeId)
                    .Distinct()
                    .CountAsync(cancellationToken),

                NoteTypeCount = await _context.SunHotelsNoteTypes
                    .Select(n => n.NoteTypeId)
                    .Distinct()
                    .CountAsync(cancellationToken),

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

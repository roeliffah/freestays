using FreeStays.Application.DTOs.SunHotels;
using FreeStays.Domain.Entities.Cache;

namespace FreeStays.Application.Common.Interfaces;

/// <summary>
/// SunHotels cache verilerine erişim sağlayan servis interface'i
/// </summary>
public interface ISunHotelsCacheService
{
    // Destinations
    Task<List<SunHotelsDestinationCache>> GetAllDestinationsAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsDestinationCache?> GetDestinationByIdAsync(int externalId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsDestinationCache>> SearchDestinationsAsync(string searchTerm, CancellationToken cancellationToken = default);

    // Resorts
    Task<List<SunHotelsResortCache>> GetAllResortsAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsResortCache?> GetResortByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);
    Task<List<SunHotelsResortCache>> GetResortsByDestinationAsync(int destinationId, string language = "en", CancellationToken cancellationToken = default);
    Task<List<SunHotelsResortCache>> SearchResortsAsync(string searchTerm, string language = "en", CancellationToken cancellationToken = default);

    // Meals
    Task<List<SunHotelsMealCache>> GetAllMealsAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsMealCache?> GetMealByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);

    // Room Types
    Task<List<SunHotelsRoomTypeCache>> GetAllRoomTypesAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsRoomTypeCache?> GetRoomTypeByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);

    // Features
    Task<List<SunHotelsFeatureCache>> GetAllFeaturesAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsFeatureCache?> GetFeatureByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);
    Task<List<SunHotelsFeatureCache>> GetFeaturesByTypeAsync(string featureType, string language = "en", CancellationToken cancellationToken = default);

    // Themes
    Task<List<SunHotelsThemeCache>> GetAllThemesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsThemeCache?> GetThemeByIdAsync(int externalId, CancellationToken cancellationToken = default);

    // Languages
    Task<List<SunHotelsLanguageCache>> GetAllLanguagesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsLanguageCache?> GetLanguageByCodeAsync(string code, CancellationToken cancellationToken = default);

    // Transfer Types
    Task<List<SunHotelsTransferTypeCache>> GetAllTransferTypesAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsTransferTypeCache?> GetTransferTypeByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);

    // Note Types
    Task<List<SunHotelsNoteTypeCache>> GetAllNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsNoteTypeCache?> GetNoteTypeByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);

    // Hotels
    Task<List<SunHotelsHotelCache>> GetAllHotelsAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsHotelCache?> GetHotelByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);
    Task<Dictionary<int, SunHotelsHotelCache>> GetHotelsByIdsAsync(IEnumerable<int> hotelIds, string language = "en", CancellationToken cancellationToken = default);

    /// <summary>
    /// DB'den direkt otelleri çek (cache bypass) - Fallback için kullanılır
    /// </summary>
    Task<Dictionary<int, SunHotelsHotelCache>> GetHotelsByIdsFromDbAsync(IEnumerable<int> hotelIds, string language = "en", CancellationToken cancellationToken = default);

    Task<Dictionary<int, SunHotelsResortCache>> GetResortsByIdsAsync(IEnumerable<int> resortIds, string language = "en", CancellationToken cancellationToken = default);
    Task<Dictionary<int, SunHotelsMealCache>> GetMealsByIdsAsync(IEnumerable<int> mealIds, string language = "en", CancellationToken cancellationToken = default);
    Task<Dictionary<int, SunHotelsRoomTypeCache>> GetRoomTypesByIdsAsync(IEnumerable<int> roomTypeIds, string language = "en", CancellationToken cancellationToken = default);
    Task<List<SunHotelsHotelCache>> GetHotelsByDestinationAsync(int destinationId, string language = "en", CancellationToken cancellationToken = default);
    Task<List<SunHotelsHotelCache>> GetHotelsByResortAsync(int resortId, string language = "en", CancellationToken cancellationToken = default);
    Task<List<SunHotelsHotelCache>> SearchHotelsAsync(string searchTerm, string language = "en", CancellationToken cancellationToken = default);
    Task<(List<SunHotelsHotelCache> Hotels, int TotalCount)> GetHotelsPaginatedAsync(int page, int pageSize, string language = "en", int? destinationId = null, int? resortId = null, int? minStars = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gelişmiş otel arama - Statik cache tablolardan
    /// Tema, konum, ülke, yemek türü, özellik gibi kriterlere göre arama yapar
    /// </summary>
    Task<HotelSearchResponse> SearchHotelsAdvancedAsync(HotelSearchRequest request, CancellationToken cancellationToken = default);

    // Rooms
    Task<List<SunHotelsRoomCache>> GetAllRoomsAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<SunHotelsRoomCache?> GetRoomByIdAsync(int externalId, string language = "en", CancellationToken cancellationToken = default);
    Task<List<SunHotelsRoomCache>> GetRoomsByHotelAsync(int hotelId, string language = "en", CancellationToken cancellationToken = default);

    // Cache Statistics
    Task<SunHotelsCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastSyncTimeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache istatistikleri
/// </summary>
public class SunHotelsCacheStatistics
{
    public int DestinationCount { get; set; }
    public int ResortCount { get; set; }
    public int MealCount { get; set; }
    public int RoomTypeCount { get; set; }
    public int FeatureCount { get; set; }
    public int ThemeCount { get; set; }
    public int LanguageCount { get; set; }
    public int TransferTypeCount { get; set; }
    public int NoteTypeCount { get; set; }
    public int HotelCount { get; set; }
    public int RoomCount { get; set; }
    public DateTime? LastSyncTime { get; set; }
}

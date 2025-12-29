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
    Task<List<SunHotelsResortCache>> GetAllResortsAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsResortCache?> GetResortByIdAsync(int externalId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsResortCache>> GetResortsByDestinationAsync(int destinationId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsResortCache>> SearchResortsAsync(string searchTerm, CancellationToken cancellationToken = default);

    // Meals
    Task<List<SunHotelsMealCache>> GetAllMealsAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsMealCache?> GetMealByIdAsync(int externalId, CancellationToken cancellationToken = default);

    // Room Types
    Task<List<SunHotelsRoomTypeCache>> GetAllRoomTypesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsRoomTypeCache?> GetRoomTypeByIdAsync(int externalId, CancellationToken cancellationToken = default);

    // Features
    Task<List<SunHotelsFeatureCache>> GetAllFeaturesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsFeatureCache?> GetFeatureByIdAsync(int externalId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsFeatureCache>> GetFeaturesByTypeAsync(string featureType, CancellationToken cancellationToken = default);

    // Themes
    Task<List<SunHotelsThemeCache>> GetAllThemesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsThemeCache?> GetThemeByIdAsync(int externalId, CancellationToken cancellationToken = default);

    // Languages
    Task<List<SunHotelsLanguageCache>> GetAllLanguagesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsLanguageCache?> GetLanguageByCodeAsync(string code, CancellationToken cancellationToken = default);

    // Transfer Types
    Task<List<SunHotelsTransferTypeCache>> GetAllTransferTypesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsTransferTypeCache?> GetTransferTypeByIdAsync(int externalId, CancellationToken cancellationToken = default);

    // Note Types
    Task<List<SunHotelsNoteTypeCache>> GetAllNoteTypesAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsNoteTypeCache?> GetNoteTypeByIdAsync(int externalId, CancellationToken cancellationToken = default);

    // Hotels
    Task<List<SunHotelsHotelCache>> GetAllHotelsAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsHotelCache?> GetHotelByIdAsync(int externalId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsHotelCache>> GetHotelsByDestinationAsync(int destinationId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsHotelCache>> GetHotelsByResortAsync(int resortId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsHotelCache>> SearchHotelsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<(List<SunHotelsHotelCache> Hotels, int TotalCount)> GetHotelsPaginatedAsync(int page, int pageSize, int? destinationId = null, int? resortId = null, int? minStars = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gelişmiş otel arama - Statik cache tablolardan
    /// Tema, konum, ülke, yemek türü, özellik gibi kriterlere göre arama yapar
    /// </summary>
    Task<HotelSearchResponse> SearchHotelsAdvancedAsync(HotelSearchRequest request, CancellationToken cancellationToken = default);

    // Rooms
    Task<List<SunHotelsRoomCache>> GetAllRoomsAsync(CancellationToken cancellationToken = default);
    Task<SunHotelsRoomCache?> GetRoomByIdAsync(int externalId, CancellationToken cancellationToken = default);
    Task<List<SunHotelsRoomCache>> GetRoomsByHotelAsync(int hotelId, CancellationToken cancellationToken = default);

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

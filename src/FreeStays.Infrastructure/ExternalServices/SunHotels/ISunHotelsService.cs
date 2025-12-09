using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;

namespace FreeStays.Infrastructure.ExternalServices.SunHotels;

/// <summary>
/// SunHotels XML API v15 entegrasyon servisi
/// Static ve Non-Static API'leri birlikte kullanır
/// </summary>
public interface ISunHotelsService
{
    #region Static Data - Önbelleğe alınmalı
    
    /// <summary>
    /// Destinasyonları getirir
    /// </summary>
    Task<List<SunHotelsDestination>> GetDestinationsAsync(string? language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resort/Bölgeleri getirir
    /// </summary>
    Task<List<SunHotelsResort>> GetResortsAsync(string? destinationId = null, string? language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Yemek tiplerini getirir (Kahvaltı, Yarım Pansiyon vb.)
    /// </summary>
    Task<List<SunHotelsMeal>> GetMealsAsync(string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Oda tiplerini getirir (Single, Double, Suite vb.)
    /// </summary>
    Task<List<SunHotelsRoomType>> GetRoomTypesAsync(string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Otel/Oda özelliklerini getirir (Havuz, WiFi, Klima vb.)
    /// </summary>
    Task<List<SunHotelsFeature>> GetFeaturesAsync(string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Desteklenen dilleri getirir
    /// </summary>
    Task<List<SunHotelsLanguage>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Otel temalarını getirir (Aile, Romantik vb.)
    /// </summary>
    Task<List<SunHotelsTheme>> GetThemesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transfer tiplerini getirir
    /// </summary>
    Task<List<SunHotelsTransferType>> GetTransferTypesAsync(string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Otel not tiplerini getirir
    /// </summary>
    Task<List<SunHotelsNoteType>> GetHotelNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Oda not tiplerini getirir
    /// </summary>
    Task<List<SunHotelsNoteType>> GetRoomNoteTypesAsync(string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Statik otel ve oda verilerini getirir (büyük veri, cache için)
    /// </summary>
    Task<List<SunHotelsStaticHotel>> GetStaticHotelsAndRoomsAsync(
        string? destination = null,
        string? hotelIds = null,
        string? resortIds = null,
        string language = "en",
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Hotel Search (V3)
    
    /// <summary>
    /// Otel araması yapar (V3 - Gelişmiş)
    /// </summary>
    Task<List<SunHotelsSearchResultV3>> SearchHotelsV3Async(SunHotelsSearchRequestV3 request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Eski versiyon arama (geriye uyumluluk için)
    /// </summary>
    Task<List<SunHotelsSearchResult>> SearchHotelsAsync(SunHotelsSearchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Otel detaylarını getirir
    /// </summary>
    Task<SunHotelsSearchResultV3?> GetHotelDetailsAsync(int hotelId, DateTime checkIn, DateTime checkOut, int adults, int children = 0, string currency = "EUR", CancellationToken cancellationToken = default);
    
    #endregion
    
    #region PreBook (V3)
    
    /// <summary>
    /// Ön rezervasyon yapar (V3 - Tax ve Fee bilgisi ile)
    /// </summary>
    Task<SunHotelsPreBookResultV3> PreBookV3Async(SunHotelsPreBookRequestV3 request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Eski versiyon ön rezervasyon (geriye uyumluluk için)
    /// </summary>
    Task<SunHotelsPreBookResult> PreBookAsync(SunHotelsPreBookRequest request, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Booking (V3)
    
    /// <summary>
    /// Rezervasyon yapar (V3)
    /// </summary>
    Task<SunHotelsBookResultV3> BookV3Async(SunHotelsBookRequestV3 request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Eski versiyon rezervasyon (geriye uyumluluk için)
    /// </summary>
    Task<SunHotelsBookResult> BookAsync(SunHotelsBookRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rezervasyonu iptal eder
    /// </summary>
    Task<SunHotelsCancelResult> CancelBookingAsync(string bookingId, string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rezervasyon bilgilerini sorgular (V3)
    /// </summary>
    Task<List<SunHotelsBookingInfo>> GetBookingInformationAsync(SunHotelsGetBookingRequest request, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Amendment (Değişiklik)
    
    /// <summary>
    /// Rezervasyon değişikliği için fiyat sorgular
    /// </summary>
    Task<SunHotelsAmendmentPriceResult> GetAmendmentPriceAsync(SunHotelsAmendmentPriceRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rezervasyon değişikliği yapar
    /// </summary>
    Task<SunHotelsAmendmentResult> AmendBookingAsync(SunHotelsAmendmentRequest request, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Special Request
    
    /// <summary>
    /// Özel istek günceller
    /// </summary>
    Task<SunHotelsSpecialRequestResult> UpdateSpecialRequestAsync(string bookingId, string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Özel isteği getirir
    /// </summary>
    Task<SunHotelsSpecialRequestResult> GetSpecialRequestAsync(string bookingId, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Transfers
    
    /// <summary>
    /// Transfer araması yapar (V2)
    /// </summary>
    Task<List<SunHotelsTransferSearchResult>> SearchTransfersAsync(SunHotelsTransferSearchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transfer ekler (V2)
    /// </summary>
    Task<SunHotelsAddTransferResult> AddTransferAsync(SunHotelsAddTransferRequestV2 request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transfer iptal eder
    /// </summary>
    Task<SunHotelsCancelTransferResult> CancelTransferAsync(string transferBookingId, string email, string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transfer rezervasyon bilgilerini sorgular
    /// </summary>
    Task<List<SunHotelsBookingInfo>> GetTransferBookingInformationAsync(
        string? bookingId = null,
        DateTime? createdDateFrom = null,
        DateTime? createdDateTo = null,
        DateTime? arrivalDateFrom = null,
        DateTime? arrivalDateTo = null,
        string language = "en",
        CancellationToken cancellationToken = default);
    
    #endregion
}

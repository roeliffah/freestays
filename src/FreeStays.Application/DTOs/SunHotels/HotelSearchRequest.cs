namespace FreeStays.Application.DTOs.SunHotels;

/// <summary>
/// Hotel arama request DTO - Next.js'ten gelecek
/// </summary>
public class HotelSearchRequest
{
    /// <summary>
    /// Dil kodu (tr, en, de, fr vs.) - Sabit gelecek
    /// </summary>
    public string Language { get; set; } = "en";

    #region Dinamik Arama Kriterleri (Tarih varsa non-static API kullanılacak)

    /// <summary>
    /// Check-in tarihi (varsa dinamik arama)
    /// </summary>
    public DateTime? CheckInDate { get; set; }

    /// <summary>
    /// Check-out tarihi (varsa dinamik arama)
    /// </summary>
    public DateTime? CheckOutDate { get; set; }

    /// <summary>
    /// Yetişkin sayısı
    /// </summary>
    public int? Adults { get; set; }

    /// <summary>
    /// Çocuk sayısı
    /// </summary>
    public int? Children { get; set; }

    /// <summary>
    /// Oda sayısı
    /// </summary>
    public int? NumberOfRooms { get; set; }

    /// <summary>
    /// Para birimi (EUR, USD, TRY vs.)
    /// </summary>
    public string? Currency { get; set; }

    #endregion

    #region Statik Arama Kriterleri (Cache tablolardan arama)

    /// <summary>
    /// Destinasyon ID'leri
    /// </summary>
    public List<string>? DestinationIds { get; set; }

    /// <summary>
    /// Resort ID'leri
    /// </summary>
    public List<int>? ResortIds { get; set; }

    /// <summary>
    /// Ülke kodları (TR, GR, ES vs.)
    /// </summary>
    public List<string>? CountryCodes { get; set; }

    /// <summary>
    /// Ülke isimleri
    /// </summary>
    public List<string>? CountryNames { get; set; }

    /// <summary>
    /// Tema ID'leri
    /// </summary>
    public List<int>? ThemeIds { get; set; }

    /// <summary>
    /// Özellik ID'leri (havuz, spa, wifi vs.)
    /// </summary>
    public List<int>? FeatureIds { get; set; }

    /// <summary>
    /// Yemek türü ID'leri
    /// </summary>
    public List<int>? MealIds { get; set; }

    /// <summary>
    /// Minimum yıldız sayısı
    /// </summary>
    public int? MinStars { get; set; }

    /// <summary>
    /// Maximum yıldız sayısı
    /// </summary>
    public int? MaxStars { get; set; }

    /// <summary>
    /// Otel adı / Serbest metin arama
    /// </summary>
    public string? SearchTerm { get; set; }

    #endregion

    #region Pagination

    /// <summary>
    /// Sayfa numarası (default: 1)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Sayfa başına kayıt sayısı (default: 20)
    /// </summary>
    public int PageSize { get; set; } = 20;

    #endregion

    /// <summary>
    /// Tarih kriteri olup olmadığını kontrol eder
    /// </summary>
    public bool HasDateCriteria => CheckInDate.HasValue && CheckOutDate.HasValue;
}

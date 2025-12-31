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

    /// <summary>
    /// Çocuk yaşları (virgülle ayrılmış: "5,8,12")
    /// </summary>
    public string? ChildrenAges { get; set; }

    /// <summary>
    /// Bebek sayısı
    /// </summary>
    public int? Infant { get; set; }

    /// <summary>
    /// Müşteri ülkesi (ISO kodu: TR, GB, DE vs.)
    /// </summary>
    public string? CustomerCountry { get; set; }

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
    /// Otel ID'leri
    /// </summary>
    public List<int>? HotelIds { get; set; }

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

    /// <summary>
    /// Konaklama tipleri (virgülle ayrılmış)
    /// </summary>
    public string? AccommodationTypes { get; set; }

    /// <summary>
    /// Kesin destinasyon eşleşmesi (true/false)
    /// </summary>
    public bool? ExactDestinationMatch { get; set; }

    /// <summary>
    /// Süper fırsatları engelle
    /// </summary>
    public bool? BlockSuperdeal { get; set; }

    /// <summary>
    /// Referans noktası enlemi (latitude)
    /// </summary>
    public double? ReferencePointLatitude { get; set; }

    /// <summary>
    /// Referans noktası boylamı (longitude)
    /// </summary>
    public double? ReferencePointLongitude { get; set; }

    /// <summary>
    /// Referans noktasından maksimum mesafe (km)
    /// </summary>
    public int? MaxDistanceFromReferencePoint { get; set; }

    /// <summary>
    /// Minimum fiyat
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maksimum fiyat
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Paylaşımlı odaları hariç tut
    /// </summary>
    public bool? ExcludeSharedRooms { get; set; }

    /// <summary>
    /// Paylaşımlı tesisleri hariç tut
    /// </summary>
    public bool? ExcludeSharedFacilities { get; set; }

    /// <summary>
    /// Öncelikli otel ID'leri (virgülle ayrılmış)
    /// </summary>
    public string? PrioritizedHotelIds { get; set; }

    /// <summary>
    /// Toplu işlemde toplam oda sayısı
    /// </summary>
    public int? TotalRoomsInBatch { get; set; }

    /// <summary>
    /// Ödeme yöntemi ID'si
    /// </summary>
    public int? PaymentMethodId { get; set; }

    /// <summary>
    /// Koordinatları göster
    /// </summary>
    public bool? ShowCoordinates { get; set; }

    /// <summary>
    /// Yorumları göster
    /// </summary>
    public bool? ShowReviews { get; set; }

    /// <summary>
    /// Oda tipi adını göster
    /// </summary>
    public bool? ShowRoomTypeName { get; set; }

    /// <summary>
    /// Sıralama kriteri (price, name, stars, distance)
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sıralama yönü (asc, desc)
    /// </summary>
    public string? SortOrder { get; set; }

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

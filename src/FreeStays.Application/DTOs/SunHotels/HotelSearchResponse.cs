using FreeStays.Domain.Entities.Cache;

namespace FreeStays.Application.DTOs.SunHotels;

/// <summary>
/// Hotel arama response DTO
/// </summary>
public class HotelSearchResponse
{
    /// <summary>
    /// Bulunan oteller
    /// </summary>
    public List<HotelSearchResultDto> Hotels { get; set; } = new();

    /// <summary>
    /// Toplam kayıt sayısı
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Toplam sayfa sayısı
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Mevcut sayfa numarası
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Sayfa başına kayıt sayısı
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Arama tipi (static/dynamic)
    /// </summary>
    public string SearchType { get; set; } = "static";

    /// <summary>
    /// Fiyat bilgisi var mı? (Tarih seçilmişse true)
    /// </summary>
    public bool HasPricing { get; set; }

    /// <summary>
    /// Fiyat mesajı (Tarih seçilmemişse "Fiyat için tarih seçin" gibi)
    /// </summary>
    public string? PriceMessage { get; set; }
}

/// <summary>
/// Arama sonucu tek bir otel DTO
/// </summary>
public class HotelSearchResultDto
{
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public int Category { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int ResortId { get; set; }
    public string ResortName { get; set; } = string.Empty;
    public List<int> ThemeIds { get; set; } = new();
    public List<int> FeatureIds { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    /// <summary>
    /// Dinamik arama için fiyat bilgisi (varsa)
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Para birimi (EUR, USD, vs.)
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// İnceleme puanı (0-10)
    /// </summary>
    public decimal? ReviewScore { get; set; }

    /// <summary>
    /// İnceleme sayısı
    /// </summary>
    public int? ReviewCount { get; set; }

    /// <summary>
    /// Giriş tarihi (dinamik arama için)
    /// </summary>
    public string? CheckInDate { get; set; }

    /// <summary>
    /// Çıkış tarihi (dinamik arama için)
    /// </summary>
    public string? CheckOutDate { get; set; }

    /// <summary>
    /// Odalar (dinamik arama için)
    /// </summary>
    public List<HotelRoomDto>? Rooms { get; set; }

    /// <summary>
    /// Fiyat ekran metni ("Fiyat için tarih seçin" veya fiyat)
    /// </summary>
    public string? PriceDisplay { get; set; }

    /// <summary>
    /// Cache entity'den map etme
    /// </summary>
    public static HotelSearchResultDto FromCache(SunHotelsHotelCache hotel)
    {
        return new HotelSearchResultDto
        {
            HotelId = hotel.HotelId,
            Name = hotel.Name,
            Description = hotel.Description,
            Address = hotel.Address,
            City = hotel.City,
            Country = hotel.Country,
            CountryCode = hotel.CountryCode,
            Category = hotel.Category,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            ResortId = hotel.ResortId,
            ResortName = hotel.ResortName,
            ThemeIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(hotel.ThemeIds) ?? new(),
            FeatureIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(hotel.FeatureIds) ?? new(),
            ImageUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(hotel.ImageUrls) ?? new(),
            Phone = hotel.Phone,
            Email = hotel.Email,
            Website = hotel.Website
        };
    }
}

/// <summary>
/// Otel odası DTO (dinamik arama için)
/// </summary>
public class HotelRoomDto
{
    public string RoomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RoomTypeName { get; set; }
    public string? MealName { get; set; }
    public decimal Price { get; set; }
    public int? AvailableRooms { get; set; }
    public bool? IsRefundable { get; set; }
    public bool? IsSuperDeal { get; set; }
}

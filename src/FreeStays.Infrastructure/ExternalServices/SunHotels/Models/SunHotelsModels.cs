namespace FreeStays.Infrastructure.ExternalServices.SunHotels.Models;

public class SunHotelsConfig
{
    public string BaseUrl { get; set; } = "http://xml.sunhotels.net/15/PostGet/NonStaticXMLAPI.asmx";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? AffiliateCode { get; set; }
}

public class SunHotelsDestination
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryId { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
}

public class SunHotelsSearchRequest
{
    public string DestinationId { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string Currency { get; set; } = "EUR";
}

public class SunHotelsSearchResult
{
    public string HotelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Category { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal MinPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<SunHotelsImage> Images { get; set; } = new();
    public List<SunHotelsRoom> Rooms { get; set; } = new();
}

public class SunHotelsImage
{
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class SunHotelsRoom
{
    public string RoomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
}

public class SunHotelsPreBookRequest
{
    public string HotelId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
}

public class SunHotelsPreBookResult
{
    public string PreBookCode { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class SunHotelsBookRequest
{
    public string PreBookCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public string? SpecialRequests { get; set; }
}

public class SunHotelsBookResult
{
    public bool Success { get; set; }
    public string BookingId { get; set; } = string.Empty;
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SunHotelsCancelResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
}

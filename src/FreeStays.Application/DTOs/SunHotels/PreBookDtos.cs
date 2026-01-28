namespace FreeStays.Application.DTOs.SunHotels;

/// <summary>
/// SunHotels PreBook V3 Request DTO
/// </summary>
public record SunHotelsPreBookRequest(
    string Currency,
    string Language,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Rooms,
    int Adults,
    int Children,
    string ChildrenAges,
    int? Infant,
    int MealId,
    string CustomerCountry,
    bool B2C,
    decimal SearchPrice,
    int RoomId,
    int HotelId,
    int RoomTypeId,
    bool BlockSuperDeal = false,
    bool ShowPriceBreakdown = true
);

/// <summary>
/// SunHotels PreBook V3 Response - Fiyat Bilgileri
/// </summary>
public record SunHotelsPreBookResponse(
    decimal Tax,
    string TaxCurrency,
    List<SunHotelsPreBookFee> Fees
);

/// <summary>
/// SunHotels PreBook Fee Detayı
/// </summary>
public record SunHotelsPreBookFee(
    string Name,
    decimal Amount,
    string Currency,
    bool IncludedInPrice
);

/// <summary>
/// BookV3 için nihai rezervasyon request'i
/// </summary>
public record SunHotelsBookV3Request(
    string Currency,
    string Language,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Rooms,
    int Adults,
    int Children,
    string ChildrenAges,
    int? Infant,
    int MealId,
    string CustomerCountry,
    int RoomId,
    int HotelId,
    int RoomTypeId,
    int SearchPrice,
    decimal FinalPrice,
    string GuestName,
    string GuestEmail,
    string GuestPhone,
    string GuestCountry,
    string SpecialRequests = ""
);

/// <summary>
/// BookV3 Response - Reservation Details
/// </summary>
public record SunHotelsBookV3Response(
    int ReservationId,
    string ConfirmationCode,
    string Status,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    decimal TotalPrice,
    string Currency,
    string Message
);

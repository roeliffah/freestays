namespace FreeStays.Infrastructure.ExternalServices.SunHotels.Models;

#region Static Data Models

/// <summary>
/// Yemek tipi (Kahvaltı, Yarım Pansiyon, Tam Pansiyon vb.)
/// </summary>
public class SunHotelsMeal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
}

/// <summary>
/// Oda tipi (Single, Double, Suite vb.)
/// </summary>
public class SunHotelsRoomType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Otel/Oda özelliği (Havuz, WiFi, Klima vb.)
/// </summary>
public class SunHotelsFeature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Desteklenen dil
/// </summary>
public class SunHotelsLanguage
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Otel teması (Aile, Romantik, İş vb.)
/// </summary>
public class SunHotelsTheme
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
}

/// <summary>
/// Resort/Bölge bilgisi
/// </summary>
public class SunHotelsResort
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DestinationId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

/// <summary>
/// Otel not tipi
/// </summary>
public class SunHotelsNoteType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Transfer tipi
/// </summary>
public class SunHotelsTransferType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

#region Search V3 Models

/// <summary>
/// Gelişmiş arama isteği (V3)
/// </summary>
public class SunHotelsSearchRequestV3
{
    public string DestinationId { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int NumberOfRooms { get; set; } = 1;
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? ChildrenAges { get; set; } // Virgülle ayrılmış yaşlar: "5,8,12"
    public int Infant { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Language { get; set; } = "en";
    public string? CustomerCountry { get; set; }
    public bool B2C { get; set; }

    // Filtreler
    public string? HotelIds { get; set; }
    public string? ResortIds { get; set; }
    public string? MealIds { get; set; }
    public string? FeatureIds { get; set; }
    public string? ThemeIds { get; set; }
    public int? MinStarRating { get; set; }
    public int? MaxStarRating { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // Konum bazlı arama
    public double? ReferenceLatitude { get; set; }
    public double? ReferenceLongitude { get; set; }
    public int? MaxDistanceKm { get; set; }

    // Sıralama
    public string SortBy { get; set; } = "price"; // price, name, stars, distance
    public string SortOrder { get; set; } = "asc"; // asc, desc

    // Özel filtreler
    public bool ExcludeSharedRooms { get; set; }
    public bool ExcludeSharedFacilities { get; set; }
    public bool ShowCoordinates { get; set; } = true;
    public bool ShowReviews { get; set; } = true;
    public bool ShowRoomTypeName { get; set; } = true;
    public int? PaymentMethodId { get; set; }
}

/// <summary>
/// Gelişmiş arama sonucu (V3)
/// </summary>
public class SunHotelsSearchResultV3
{
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public int Category { get; set; } // Yıldız sayısı
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? GiataCode { get; set; }
    public int ResortId { get; set; }
    public string ResortName { get; set; } = string.Empty;

    // Fiyat bilgileri
    public decimal MinPrice { get; set; }
    public string Currency { get; set; } = string.Empty;

    // Review bilgileri
    public double? ReviewScore { get; set; }
    public int? ReviewCount { get; set; }

    // İlişkili veriler
    public List<SunHotelsImage> Images { get; set; } = new();
    public List<SunHotelsRoomV3> Rooms { get; set; } = new();
    public List<int> FeatureIds { get; set; } = new();
    public List<int> ThemeIds { get; set; } = new();
}

/// <summary>
/// Gelişmiş oda bilgisi (V3)
/// </summary>
public class SunHotelsRoomV3
{
    public int RoomId { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MealId { get; set; }
    public string MealName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public List<int> PaymentMethodIds { get; set; } = new();
    public List<SunHotelsCancellationPolicy> CancellationPolicies { get; set; } = new();
    public DateTime? EarliestNonFreeCancellationDate { get; set; }
    public bool IsSuperDeal { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int AvailableRooms { get; set; }
}

/// <summary>
/// İptal politikası
/// </summary>
public class SunHotelsCancellationPolicy
{
    public DateTime FromDate { get; set; }
    public decimal Percentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public int NightsCharged { get; set; }
}

#endregion

#region PreBook V3 Models

/// <summary>
/// Ön rezervasyon isteği (V3)
/// </summary>
public class SunHotelsPreBookRequestV3
{
    public int HotelId { get; set; }
    public int RoomId { get; set; }
    public int RoomTypeId { get; set; }
    public int MealId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Rooms { get; set; } = 1;
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? ChildrenAges { get; set; }
    public int Infant { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Language { get; set; } = "en";
    public string? CustomerCountry { get; set; }
    public bool B2C { get; set; }
    public decimal SearchPrice { get; set; }
    public bool BlockSuperDeal { get; set; }
    public bool ShowPriceBreakdown { get; set; } = true;
}

/// <summary>
/// Ön rezervasyon sonucu (V3 - Tax ve Fee bilgisi ile)
/// </summary>
public class SunHotelsPreBookResultV3
{
    public string PreBookCode { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal NetPrice { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? FeeAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool PriceChanged { get; set; }
    public decimal? OriginalPrice { get; set; }
    public List<SunHotelsPriceBreakdown> PriceBreakdown { get; set; } = new();
    public List<SunHotelsCancellationPolicy> CancellationPolicies { get; set; } = new();
    public DateTime? EarliestNonFreeCancellationDateCET { get; set; }
    public DateTime? EarliestNonFreeCancellationDateLocal { get; set; }
    public string? HotelNotes { get; set; }
    public string? RoomNotes { get; set; }
}

/// <summary>
/// Fiyat kırılımı
/// </summary>
public class SunHotelsPriceBreakdown
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}

#endregion

#region Book V3 Models

/// <summary>
/// Rezervasyon isteği (V3)
/// </summary>
public class SunHotelsBookRequestV3
{
    public string PreBookCode { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public int MealId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Rooms { get; set; } = 1;
    public int Adults { get; set; }
    public int Children { get; set; }
    public int Infant { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Language { get; set; } = "en";
    public string Email { get; set; } = string.Empty;
    public string? YourRef { get; set; }
    public string? SpecialRequest { get; set; }
    public string? CustomerCountry { get; set; }
    public bool B2C { get; set; }
    public string? InvoiceRef { get; set; }
    public decimal? CommissionAmount { get; set; }

    // Yetişkin misafirler (max 9)
    public List<SunHotelsGuest> AdultGuests { get; set; } = new();

    // Çocuk misafirler (max 9)
    public List<SunHotelsChildGuest> ChildrenGuests { get; set; } = new();

    // Ödeme bilgileri
    public int PaymentMethodId { get; set; }
    public SunHotelsCreditCard? CreditCard { get; set; }
    public string? CustomerEmail { get; set; }
}

/// <summary>
/// Misafir bilgisi
/// </summary>
public class SunHotelsGuest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// Çocuk misafir bilgisi
/// </summary>
public class SunHotelsChildGuest : SunHotelsGuest
{
    public int Age { get; set; }
}

/// <summary>
/// Kredi kartı bilgisi
/// </summary>
public class SunHotelsCreditCard
{
    public string CardType { get; set; } = string.Empty; // VISA, MC, AMEX
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolder { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
    public string ExpYear { get; set; } = string.Empty;
    public string ExpMonth { get; set; } = string.Empty;
}

/// <summary>
/// Rezervasyon sonucu (V3)
/// </summary>
public class SunHotelsBookResultV3
{
    public bool Success { get; set; }
    public string? BookingNumber { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string? HotelPhone { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public int MealId { get; set; }
    public string MealName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int NumberOfRooms { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string? Voucher { get; set; }
    public string? YourRef { get; set; }
    public string? InvoiceRef { get; set; }
    public List<SunHotelsCancellationPolicy> CancellationPolicies { get; set; } = new();
    public DateTime? EarliestNonFreeCancellationDateCET { get; set; }
    public DateTime? EarliestNonFreeCancellationDateLocal { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion

#region Transfer Models

/// <summary>
/// Transfer arama isteği
/// </summary>
public class SunHotelsTransferSearchRequest
{
    public int? HotelId { get; set; }
    public int? RoomId { get; set; }
    public string? BookingId { get; set; }
    public int? ResortId { get; set; }
    public int? TransferId { get; set; }
    public string? GiataCode { get; set; }
    public DateTime ArrivalDate { get; set; }
    public string ArrivalTime { get; set; } = string.Empty; // HH:mm format
    public DateTime? ReturnDepartureDate { get; set; }
    public string? ReturnDepartureTime { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Language { get; set; } = "en";
}

/// <summary>
/// Transfer arama sonucu
/// </summary>
public class SunHotelsTransferSearchResult
{
    public int TransferId { get; set; }
    public int TransferTypeId { get; set; }
    public string TransferTypeName { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string DropoffLocation { get; set; } = string.Empty;
    public int MaxPassengers { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IncludesReturn { get; set; }
    public string? VehicleType { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Transfer ekleme isteği (V2)
/// </summary>
public class SunHotelsAddTransferRequestV2
{
    public string BookingId { get; set; } = string.Empty;
    public int? HotelId { get; set; }
    public string? HotelGiataCode { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string? HotelAddress { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactCellphone { get; set; } = string.Empty;
    public string AirlineCruiseline { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string? OriginTerminal { get; set; }
    public string? DepartureIataCode { get; set; }
    public string DepartureTime { get; set; } = string.Empty;
    public string ArrivalTime { get; set; } = string.Empty;
    public DateTime ArrivalDate { get; set; }
    public int Passengers { get; set; }
    public int TransferId { get; set; }
    public bool ReturnTransfer { get; set; }
    public string? ReturnAirlineCruiseline { get; set; }
    public string? ReturnFlightNumber { get; set; }
    public DateTime? ReturnDepartureDate { get; set; }
    public string? ReturnDepartureTime { get; set; }
    public string? InvoiceRef { get; set; }
    public string? YourRef { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Language { get; set; } = "en";
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Transfer ekleme sonucu
/// </summary>
public class SunHotelsAddTransferResult
{
    public bool Success { get; set; }
    public string? TransferBookingId { get; set; }
    public string? ConfirmationNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Transfer iptal sonucu
/// </summary>
public class SunHotelsCancelTransferResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public decimal? RefundAmount { get; set; }
}

#endregion

#region Booking Information Models

/// <summary>
/// Rezervasyon sorgulama isteği (V3)
/// </summary>
public class SunHotelsGetBookingRequest
{
    public string? BookingId { get; set; }
    public string? Reference { get; set; }
    public DateTime? CreatedDateFrom { get; set; }
    public DateTime? CreatedDateTo { get; set; }
    public DateTime? ArrivalDateFrom { get; set; }
    public DateTime? ArrivalDateTo { get; set; }
    public bool ShowGuests { get; set; } = true;
    public string Language { get; set; } = "en";
}

/// <summary>
/// Rezervasyon bilgisi
/// </summary>
public class SunHotelsBookingInfo
{
    public string BookingNumber { get; set; } = string.Empty;
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string? HotelPhone { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public int NumberOfRooms { get; set; }
    public int MealId { get; set; }
    public string MealName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string Status { get; set; } = string.Empty; // Confirmed, Cancelled, etc.
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string? YourRef { get; set; }
    public string? InvoiceRef { get; set; }
    public string? Voucher { get; set; }
    public List<SunHotelsGuest> AdultGuests { get; set; } = new();
    public List<SunHotelsChildGuest> ChildrenGuests { get; set; } = new();
    public List<SunHotelsCancellationPolicy> CancellationPolicies { get; set; } = new();
    public DateTime? EarliestNonFreeCancellationDateCET { get; set; }
    public bool HasTransfer { get; set; }
}

#endregion

#region Amendment Models

/// <summary>
/// Rezervasyon değişiklik fiyat talebi
/// </summary>
public class SunHotelsAmendmentPriceRequest
{
    public string BookingId { get; set; } = string.Empty;
    public DateTime NewCheckIn { get; set; }
    public DateTime NewCheckOut { get; set; }
    public string Language { get; set; } = "en";
}

/// <summary>
/// Rezervasyon değişiklik fiyat sonucu
/// </summary>
public class SunHotelsAmendmentPriceResult
{
    public bool Success { get; set; }
    public bool AmendmentPossible { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public decimal? PriceDifference { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Message { get; set; }
}

/// <summary>
/// Rezervasyon değişiklik talebi
/// </summary>
public class SunHotelsAmendmentRequest
{
    public string BookingId { get; set; } = string.Empty;
    public int? NewRoomId { get; set; }
    public DateTime NewCheckIn { get; set; }
    public DateTime NewCheckOut { get; set; }
    public decimal MaxPrice { get; set; }
    public string Language { get; set; } = "en";
    public string BookingType { get; set; } = "instant"; // instant, request
}

/// <summary>
/// Rezervasyon değişiklik sonucu
/// </summary>
public class SunHotelsAmendmentResult
{
    public bool Success { get; set; }
    public string? NewBookingNumber { get; set; }
    public decimal? NewTotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Message { get; set; }
}

#endregion

#region Static Hotel Data

/// <summary>
/// Statik otel ve oda verisi
/// </summary>
public class SunHotelsStaticHotel
{
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public int Category { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GiataCode { get; set; }
    public int ResortId { get; set; }
    public string ResortName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public List<SunHotelsImage> Images { get; set; } = new();
    public List<SunHotelsStaticRoom> Rooms { get; set; } = new();
    public List<int> FeatureIds { get; set; } = new();
    public List<int> ThemeIds { get; set; } = new();
    public List<SunHotelsHotelNote> Notes { get; set; } = new();
}

/// <summary>
/// Statik oda verisi
/// </summary>
public class SunHotelsStaticRoom
{
    public int RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxOccupancy { get; set; }
    public int MinOccupancy { get; set; }
    public List<int> FeatureIds { get; set; } = new();
    public List<SunHotelsImage> Images { get; set; } = new();
    public List<SunHotelsRoomNote> Notes { get; set; } = new();
}

/// <summary>
/// Otel notu
/// </summary>
public class SunHotelsHotelNote
{
    public int NoteTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? EnglishText { get; set; }
}

/// <summary>
/// Oda notu
/// </summary>
public class SunHotelsRoomNote
{
    public int NoteTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? EnglishText { get; set; }
}

#endregion

#region Special Request Models

/// <summary>
/// Özel istek güncelleme
/// </summary>
public class SunHotelsSpecialRequest
{
    public string BookingId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Özel istek sonucu
/// </summary>
public class SunHotelsSpecialRequestResult
{
    public bool Success { get; set; }
    public string? CurrentText { get; set; }
    public string? Message { get; set; }
}

#endregion

#region API Response Wrappers

/// <summary>
/// Genel API yanıtı
/// </summary>
public class SunHotelsApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public SunHotelsError? Error { get; set; }
}

/// <summary>
/// API Hata bilgisi
/// </summary>
public class SunHotelsError
{
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<SunHotelsErrorAttribute> Attributes { get; set; } = new();
}

/// <summary>
/// Hata detayı
/// </summary>
public class SunHotelsErrorAttribute
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

#endregion

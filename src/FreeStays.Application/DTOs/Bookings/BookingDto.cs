namespace FreeStays.Application.DTOs.Bookings;

public record BookingDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalPrice { get; init; }
    public decimal Commission { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal CouponDiscount { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public HotelBookingDto? HotelBooking { get; init; }
    public FlightBookingDto? FlightBooking { get; init; }
    public CarRentalDto? CarRental { get; init; }
    public PaymentDto? Payment { get; init; }
}

public record HotelBookingDto
{
    public Guid Id { get; init; }
    public Guid HotelId { get; init; }
    public string? HotelName { get; init; }
    public string? RoomTypeName { get; init; }
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
    public int Adults { get; init; }
    public int Children { get; init; }
    public string? GuestName { get; init; }
    public string? ExternalBookingId { get; init; }
}

public record FlightBookingDto
{
    public Guid Id { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public string Departure { get; init; } = string.Empty;
    public string Arrival { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public int Passengers { get; init; }
    public string? Airline { get; init; }
    public string? Class { get; init; }
}

public record CarRentalDto
{
    public Guid Id { get; init; }
    public string CarType { get; init; } = string.Empty;
    public string? CarModel { get; init; }
    public string PickupLocation { get; init; } = string.Empty;
    public string DropoffLocation { get; init; } = string.Empty;
    public DateTime PickupDate { get; init; }
    public DateTime DropoffDate { get; init; }
    public string? DriverName { get; init; }
}

public record PaymentDto
{
    public Guid Id { get; init; }
    public string? StripePaymentId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
}

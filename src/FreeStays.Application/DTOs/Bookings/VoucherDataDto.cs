namespace FreeStays.Application.DTOs.Bookings;

/// <summary>
/// Voucher data response DTO
/// </summary>
public record VoucherDataDto
{
    public string BookingNumber { get; init; } = string.Empty;
    public string? HotelBookingCode { get; init; }
    public string Status { get; init; } = string.Empty;
    public VoucherHotelDto Hotel { get; init; } = null!;
    public VoucherRoomDto Room { get; init; } = null!;
    public VoucherMealDto Meal { get; init; } = null!;
    public VoucherDatesDto Dates { get; init; } = null!;
    public VoucherPriceDto Price { get; init; } = null!;
    public VoucherGuestDto Guest { get; init; } = null!;
    public VoucherCancellationDto Cancellation { get; init; } = null!;
    public List<VoucherHotelNoteDto> HotelNotes { get; init; } = new();
    public VoucherPaymentMethodDto PaymentMethod { get; init; } = null!;
    public string? YourRef { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
}

public record VoucherHotelDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
}

public record VoucherRoomDto
{
    public string Type { get; init; } = string.Empty;
    public string EnglishType { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record VoucherMealDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string EnglishName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public record VoucherDatesDto
{
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
    public DateTime BookingDate { get; init; }
    public string Timezone { get; init; } = "GMT+00:00:00";
}

public record VoucherPriceDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
}

public record VoucherGuestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
}

public record VoucherCancellationDto
{
    public decimal Percentage { get; init; }
    public string? Text { get; init; }
    public DateTime? EarliestDate { get; init; }
}

public record VoucherHotelNoteDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Text { get; init; } = string.Empty;
}

public record VoucherPaymentMethodDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

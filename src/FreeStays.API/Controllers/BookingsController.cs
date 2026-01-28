using System.Text.Json;
using FreeStays.Application.DTOs.Bookings;
using FreeStays.Application.Features.Bookings.Commands;
using FreeStays.Application.Features.Bookings.Queries;
using FreeStays.Domain.Enums;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers;

[Authorize]
public class BookingsController : BaseApiController
{
    private readonly FreeStaysDbContext _dbContext;

    public BookingsController(FreeStaysDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Kullanıcının rezervasyonlarını getir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetMyBookingsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Rezervasyon detaylarını getir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var result = await Mediator.Send(new GetBookingByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Otel rezervasyonu oluştur
    /// </summary>
    [HttpPost("hotels")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateHotelBooking([FromBody] CreateHotelBookingCommand command)
    {
        var bookingId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetBooking), new { id = bookingId }, new { id = bookingId });
    }

    /// <summary>
    /// Uçuş rezervasyonu oluştur
    /// </summary>
    [HttpPost("flights")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFlightBooking([FromBody] CreateFlightBookingRequest request)
    {
        // TODO: Implement flight booking
        return Ok(new { message = "Flight booking - to be implemented" });
    }

    /// <summary>
    /// Araç kiralama rezervasyonu oluştur
    /// </summary>
    [HttpPost("cars")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCarRentalBooking([FromBody] CreateCarRentalRequest request)
    {
        // TODO: Implement car rental booking
        return Ok(new { message = "Car rental booking - to be implemented" });
    }

    /// <summary>
    /// Rezervasyonu iptal et
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelBooking(Guid id, [FromBody] CancelBookingRequest? request)
    {
        // TODO: Implement cancel booking command
        return Ok(new { message = "Rezervasyon iptal edildi." });
    }

    /// <summary>
    /// Rezervasyon voucher verilerini getir (bookingNumber ile)
    /// </summary>
    [HttpGet("{bookingNumber}/voucher-data")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVoucherData(string bookingNumber)
    {
        // SunHotels booking code ile ara (ConfirmationCode alanı)
        var hotelBooking = await _dbContext.HotelBookings
            .Include(hb => hb.Booking)
            .Include(hb => hb.Hotel)
            .FirstOrDefaultAsync(hb => hb.ConfirmationCode == bookingNumber);

        if (hotelBooking == null)
        {
            return NotFound(new { message = $"Booking with number '{bookingNumber}' not found" });
        }

        var booking = hotelBooking.Booking;

        // Status mapping
        var status = booking.Status switch
        {
            BookingStatus.Confirmed => "confirmed",
            BookingStatus.Cancelled => "cancelled",
            BookingStatus.Refunded => "cancelled",
            _ => "pending"
        };

        // HotelNotes parsing (JSON array)
        var hotelNotes = new List<VoucherHotelNoteDto>();
        if (!string.IsNullOrEmpty(hotelBooking.HotelNotes))
        {
            try
            {
                var notesArray = JsonSerializer.Deserialize<JsonElement>(hotelBooking.HotelNotes);
                if (notesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var note in notesArray.EnumerateArray())
                    {
                        hotelNotes.Add(new VoucherHotelNoteDto
                        {
                            StartDate = note.TryGetProperty("startDate", out var sd) && DateTime.TryParse(sd.GetString(), out var startDate)
                                ? startDate
                                : hotelBooking.CheckIn,
                            EndDate = note.TryGetProperty("endDate", out var ed) && DateTime.TryParse(ed.GetString(), out var endDate)
                                ? endDate
                                : hotelBooking.CheckOut,
                            Text = note.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : ""
                        });
                    }
                }
                else if (notesArray.ValueKind == JsonValueKind.String)
                {
                    // Tek string ise
                    hotelNotes.Add(new VoucherHotelNoteDto
                    {
                        StartDate = hotelBooking.CheckIn,
                        EndDate = hotelBooking.CheckOut,
                        Text = notesArray.GetString() ?? ""
                    });
                }
            }
            catch
            {
                // Plain text olarak ekle
                hotelNotes.Add(new VoucherHotelNoteDto
                {
                    StartDate = hotelBooking.CheckIn,
                    EndDate = hotelBooking.CheckOut,
                    Text = hotelBooking.HotelNotes
                });
            }
        }

        // CancellationPolicies'den earliest date çekmeye çalış
        DateTime? earliestCancellationDate = hotelBooking.FreeCancellationDeadline ?? hotelBooking.SunHotelsBookingDate;

        var voucherData = new VoucherDataDto
        {
            BookingNumber = bookingNumber,
            HotelBookingCode = hotelBooking.ConfirmationCode,
            Status = status,
            Hotel = new VoucherHotelDto
            {
                Id = hotelBooking.ExternalHotelId,
                Name = hotelBooking.Hotel?.Name ?? $"Hotel #{hotelBooking.ExternalHotelId}",
                Address = hotelBooking.HotelAddress ?? hotelBooking.Hotel?.Address,
                Phone = hotelBooking.HotelPhone
            },
            Room = new VoucherRoomDto
            {
                Type = hotelBooking.RoomTypeName ?? "",
                EnglishType = hotelBooking.RoomTypeName ?? "",
                Count = 1 // Default 1 room
            },
            Meal = new VoucherMealDto
            {
                Id = hotelBooking.MealId,
                Name = hotelBooking.MealName ?? "",
                EnglishName = hotelBooking.MealName ?? "",
                Label = ""
            },
            Dates = new VoucherDatesDto
            {
                CheckIn = hotelBooking.CheckIn,
                CheckOut = hotelBooking.CheckOut,
                BookingDate = hotelBooking.SunHotelsBookingDate ?? booking.CreatedAt,
                Timezone = "GMT+01:00:00"
            },
            Price = new VoucherPriceDto
            {
                Amount = booking.TotalPrice,
                Currency = booking.Currency
            },
            Guest = new VoucherGuestDto
            {
                Name = hotelBooking.GuestName ?? "",
                Email = hotelBooking.GuestEmail
            },
            Cancellation = new VoucherCancellationDto
            {
                Percentage = hotelBooking.CancellationPercentage,
                Text = hotelBooking.CancellationPolicyText,
                EarliestDate = earliestCancellationDate
            },
            HotelNotes = hotelNotes,
            PaymentMethod = new VoucherPaymentMethodDto
            {
                Id = 1,
                Name = "Invoice"
            },
            YourRef = $"FS-{booking.Id}",
            CancelledAt = booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Refunded
                ? booking.UpdatedAt
                : null,
            CancellationReason = booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Refunded
                ? booking.Notes
                : null
        };

        return Ok(voucherData);
    }
}

public record CreateFlightBookingRequest(
    string DepartureAirport,
    string ArrivalAirport,
    DateTime DepartureDate,
    DateTime? ReturnDate,
    int Passengers,
    string CabinClass);

public record CreateCarRentalRequest(
    string PickupLocation,
    string DropoffLocation,
    DateTime PickupDate,
    DateTime DropoffDate,
    string CarCategory);

public record CancelBookingRequest(string? Reason);

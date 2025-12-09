using FreeStays.Application.Features.Bookings.Commands;
using FreeStays.Application.Features.Bookings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

[Authorize]
public class BookingsController : BaseApiController
{
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

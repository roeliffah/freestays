using FreeStays.Application.Features.Hotels.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

public class HotelsController : BaseApiController
{
    /// <summary>
    /// Otelleri ara
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchHotels([FromQuery] SearchHotelsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Otel detaylarını getir
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotel(Guid id)
    {
        var result = await Mediator.Send(new GetHotelByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Öne çıkan otelleri getir
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedHotels([FromQuery] int count = 10)
    {
        var result = await Mediator.Send(new GetFeaturedHotelsQuery(count));
        return Ok(result);
    }

    /// <summary>
    /// Destinasyonları getir
    /// </summary>
    [HttpGet("destinations")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDestinations([FromQuery] GetDestinationsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir destinasyonun otellerini getir
    /// </summary>
    [HttpGet("destinations/{destinationId}/hotels")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHotelsByDestination(Guid destinationId, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut, [FromQuery] int adults = 2)
    {
        var query = new SearchHotelsQuery
        {
            Destination = destinationId.ToString(),
            CheckIn = checkIn ?? DateTime.Today.AddDays(7),
            CheckOut = checkOut ?? DateTime.Today.AddDays(10),
            Guests = adults
        };
        
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Oda müsaitliğini kontrol et
    /// </summary>
    [HttpGet("{id}/rooms")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoomAvailability(Guid id, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut, [FromQuery] int adults = 2, [FromQuery] int children = 0)
    {
        // TODO: Implement room availability query
        return Ok(new { hotelId = id, message = "Room availability check - to be implemented" });
    }
}

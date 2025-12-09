using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

public class FlightsController : BaseApiController
{
    /// <summary>
    /// Uçuş ara
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchFlights([FromQuery] SearchFlightsRequest query)
    {
        // TODO: Implement Kiwi.com API integration
        return Ok(new 
        { 
            flights = new List<object>(),
            message = "Kiwi.com API integration - to be implemented"
        });
    }

    /// <summary>
    /// Uçuş detaylarını getir
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFlight(string id)
    {
        // TODO: Implement get flight details
        return Ok(new { id = id });
    }

    /// <summary>
    /// Havaalanlarını ara
    /// </summary>
    [HttpGet("airports")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAirports([FromQuery] string query)
    {
        // TODO: Implement airport search
        return Ok(new List<object>());
    }

    /// <summary>
    /// Popüler rotaları getir
    /// </summary>
    [HttpGet("popular-routes")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopularRoutes([FromQuery] int count = 10)
    {
        // TODO: Implement popular routes
        return Ok(new List<object>());
    }
}

public record SearchFlightsRequest(
    string DepartureAirport,
    string ArrivalAirport,
    DateTime DepartureDate,
    DateTime? ReturnDate,
    int Adults = 1,
    int? Children = 0,
    int? Infants = 0,
    string CabinClass = "economy",
    bool DirectFlightsOnly = false);

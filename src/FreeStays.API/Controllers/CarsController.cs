using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers;

public class CarsController : BaseApiController
{
    /// <summary>
    /// Araç ara
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCars([FromQuery] SearchCarsRequest query)
    {
        // TODO: Implement DiscoverCars API integration
        return Ok(new 
        { 
            cars = new List<object>(),
            message = "DiscoverCars API integration - to be implemented"
        });
    }

    /// <summary>
    /// Araç detaylarını getir
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCar(string id)
    {
        // TODO: Implement get car details
        return Ok(new { id = id });
    }

    /// <summary>
    /// Lokasyonları ara
    /// </summary>
    [HttpGet("locations")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchLocations([FromQuery] string query)
    {
        // TODO: Implement location search
        return Ok(new List<object>());
    }

    /// <summary>
    /// Araç kategorilerini getir
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        return Ok(new List<object>
        {
            new { id = "economy", name = "Ekonomi", description = "Küçük ve ekonomik araçlar" },
            new { id = "compact", name = "Kompakt", description = "Kompakt araçlar" },
            new { id = "midsize", name = "Orta Boy", description = "Orta boy araçlar" },
            new { id = "fullsize", name = "Tam Boy", description = "Tam boy araçlar" },
            new { id = "suv", name = "SUV", description = "Spor amaçlı araçlar" },
            new { id = "luxury", name = "Lüks", description = "Lüks araçlar" },
            new { id = "minivan", name = "Minivan", description = "Aile araçları" }
        });
    }
}

public record SearchCarsRequest(
    string PickupLocation,
    string? DropoffLocation,
    DateTime PickupDate,
    DateTime DropoffDate,
    string? Category = null,
    int? MinPassengers = null,
    bool AutomaticTransmission = false);

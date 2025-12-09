using FreeStays.Application.Features.Customers.Commands;
using FreeStays.Application.Features.Customers.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/customers")]
public class AdminCustomersController : BaseApiController
{
    /// <summary>
    /// Tüm müşterileri listele
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isBlocked = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        var result = await Mediator.Send(new GetCustomersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            IsBlocked = isBlocked
        });
        
        return Ok(result);
    }

    /// <summary>
    /// Müşteri detaylarını getir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var result = await Mediator.Send(new GetCustomerByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Müşteri bilgilerini güncelle
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await Mediator.Send(new UpdateCustomerCommand
        {
            Id = id,
            Notes = request.Notes,
            IsBlocked = request.IsBlocked ?? false
        });
        
        return Ok(result);
    }

    /// <summary>
    /// Müşteri sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        // Soft delete - just block the customer
        await Mediator.Send(new UpdateCustomerCommand
        {
            Id = id,
            IsBlocked = true,
            Notes = "Deleted by admin"
        });
        
        return NoContent();
    }

    /// <summary>
    /// Müşterinin rezervasyonlarını getir
    /// </summary>
    [HttpGet("{id}/bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerBookings(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetCustomerBookingsQuery(id));
        
        var paged = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        return Ok(new 
        { 
            items = paged,
            page = page,
            pageSize = pageSize,
            totalCount = result.Count
        });
    }
}

public record UpdateCustomerRequest(
    string? Notes,
    bool? IsBlocked,
    string? BlockReason);

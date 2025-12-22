using FreeStays.Application.DTOs.Admin;
using FreeStays.Application.Features.Admin.Commands;
using FreeStays.Application.Features.Admin.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/users")]
public class AdminUsersController : BaseApiController
{
    /// <summary>
    /// Tüm Admin/SuperAdmin kullanıcılarını listele
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await Mediator.Send(new GetAdminUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        });

        return Ok(result);
    }

    /// <summary>
    /// Admin/SuperAdmin kullanıcı detayı
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdminUser(Guid id)
    {
        var result = await Mediator.Send(new GetAdminUserByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Yeni Admin/SuperAdmin kullanıcı oluştur
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminUserRequest request)
    {
        var command = new CreateAdminUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            Name = request.Name,
            Phone = request.Phone,
            Role = request.Role  // Artık direkt kullanılabilir
        };

        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetAdminUser), new { id = result.Id }, result);
    }

    /// <summary>
    /// Admin/SuperAdmin kullanıcı bilgilerini güncelle
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAdminUser(Guid id, [FromBody] UpdateAdminUserRequestWrapper wrapper)
    {
        var request = wrapper.AsRequest();

        var command = new UpdateAdminUserCommand
        {
            Id = id,
            Name = request.Name,
            Phone = request.Phone,
            Role = request.Role,  // Artık direkt kullanılabilir
            IsActive = request.IsActive,
            NewPassword = request.NewPassword
        };

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Admin/SuperAdmin kullanıcı sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAdminUser(Guid id)
    {
        await Mediator.Send(new DeleteAdminUserCommand(id));
        return NoContent();
    }
}
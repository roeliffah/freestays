using System.Text.Json;
using System.Text.Json.Serialization;
using FreeStays.Domain.Enums;

namespace FreeStays.Application.DTOs.Admin;

public record CreateAdminUserRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }

    [JsonConverter(typeof(RoleConverter))]
    public UserRole Role { get; init; } = UserRole.Admin;  // Hem string hem number kabul eder
}

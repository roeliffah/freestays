using System.Text.Json;
using System.Text.Json.Serialization;
using FreeStays.Domain.Enums;

namespace FreeStays.Application.DTOs.Admin;

public record UpdateAdminUserRequest
{
    public string? Name { get; init; }  // Opsiyonel
    public string? Phone { get; init; }

    [JsonConverter(typeof(RoleConverter))]
    public UserRole? Role { get; init; }  // Hem string hem number kabul eder

    public bool? IsActive { get; init; }
    public string? NewPassword { get; init; }
}

// Custom JSON converter for Role that accepts both string and number
public class RoleConverter : JsonConverter<UserRole?>
{
    public override UserRole? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.Number)
        {
            var roleValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(UserRole), roleValue))
                return (UserRole)roleValue;
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var roleString = reader.GetString();
            if (string.IsNullOrWhiteSpace(roleString))
                return null;

            return roleString.ToLower() switch
            {
                "admin" => UserRole.Admin,
                "superadmin" => UserRole.SuperAdmin,
                "staff" => UserRole.Staff,
                "customer" => UserRole.Customer,
                _ => Enum.TryParse<UserRole>(roleString, true, out var result) ? result : null
            };
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, UserRole? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue((int)value.Value);
        else
            writer.WriteNullValue();
    }
}

// Wrapper class for Next.js requests that send { command: { ... } }
public record UpdateAdminUserRequestWrapper
{
    [JsonPropertyName("command")]
    public UpdateAdminUserRequest? Command { get; init; }

    // Direct properties (fallback if no wrapper)
    public string? Name { get; init; }
    public string? Phone { get; init; }

    [JsonConverter(typeof(RoleConverter))]
    public UserRole? Role { get; init; }

    public bool? IsActive { get; init; }
    public string? NewPassword { get; init; }

    // Helper to get request object whether wrapped or not
    public UpdateAdminUserRequest AsRequest()
    {
        return Command ?? new UpdateAdminUserRequest
        {
            Name = Name,
            Phone = Phone,
            Role = Role,
            IsActive = IsActive,
            NewPassword = NewPassword
        };
    }
}

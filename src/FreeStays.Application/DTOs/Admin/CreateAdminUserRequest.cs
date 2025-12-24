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

    [JsonConverter(typeof(NonNullableRoleConverter))]
    public UserRole Role { get; init; } = UserRole.Admin;  // Hem string hem number kabul eder
}

// Custom JSON converter for non-nullable Role
public class NonNullableRoleConverter : JsonConverter<UserRole>
{
    public override UserRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var roleValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(UserRole), roleValue))
                return (UserRole)roleValue;
            return UserRole.Admin; // Default
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var roleString = reader.GetString();
            if (string.IsNullOrWhiteSpace(roleString))
                return UserRole.Admin; // Default

            return roleString.ToLower() switch
            {
                "admin" => UserRole.Admin,
                "superadmin" => UserRole.SuperAdmin,
                "staff" => UserRole.Staff,
                "customer" => UserRole.Customer,
                _ => Enum.TryParse<UserRole>(roleString, true, out var result) ? result : UserRole.Admin
            };
        }

        return UserRole.Admin; // Default
    }

    public override void Write(Utf8JsonWriter writer, UserRole value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((int)value);
    }
}

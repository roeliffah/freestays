namespace FreeStays.Application.DTOs.Hotels;

public record DestinationDto
{
    public Guid Id { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsPopular { get; init; }
}

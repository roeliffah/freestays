namespace FreeStays.Application.DTOs.Hotels;

public record HotelDto
{
    public Guid Id { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Address { get; init; }
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public int Category { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public decimal? MinPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<HotelImageDto> Images { get; init; } = new();
    public List<HotelFacilityDto> Facilities { get; init; } = new();
}

public record HotelImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public int Order { get; init; }
    public string? Caption { get; init; }
}

public record HotelFacilityDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Category { get; init; }
}

public record HotelSearchResultDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public int Category { get; init; }
    public decimal? MinPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? MainImageUrl { get; init; }
}

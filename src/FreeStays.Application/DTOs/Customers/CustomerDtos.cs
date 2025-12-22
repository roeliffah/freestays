using FreeStays.Domain.Enums;

namespace FreeStays.Application.DTOs.Customers;

public record CustomerDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public UserRole Role { get; init; }
    public int TotalBookings { get; init; }
    public decimal TotalSpent { get; init; }
    public DateTime? LastBookingAt { get; init; }
    public string? Notes { get; init; }
    public bool IsBlocked { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CustomerListDto
{
    public List<CustomerDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record UpdateCustomerDto
{
    public string? Notes { get; init; }
    public bool IsBlocked { get; init; }
}

public record CustomerBookingDto
{
    public Guid Id { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
    public decimal TotalPrice { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

using FreeStays.Application.DTOs.Customers;
using FreeStays.Domain.Entities;

namespace FreeStays.Application.Features.Customers.Extensions;

public static class CustomerExtensions
{
    public static CustomerDto ToDto(this Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            UserId = customer.UserId,
            Email = customer.User?.Email ?? string.Empty,
            Name = customer.User?.Name ?? string.Empty,
            Phone = customer.User?.Phone,
            Role = customer.User?.Role ?? Domain.Enums.UserRole.Customer,
            TotalBookings = customer.TotalBookings,
            TotalSpent = customer.TotalSpent,
            LastBookingAt = customer.LastBookingAt,
            Notes = customer.Notes,
            IsBlocked = customer.IsBlocked,
            CreatedAt = customer.CreatedAt
        };
    }
}

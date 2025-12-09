using FreeStays.Application.DTOs.Customers;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Customers.Queries;

public record GetCustomersQuery : IRequest<CustomerListDto>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public bool? IsBlocked { get; init; }
}

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, CustomerListDto>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerListDto> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _customerRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.IsBlocked,
            cancellationToken);

        return new CustomerListDto
        {
            Items = items.Select(c => new CustomerDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Email = c.User?.Email ?? string.Empty,
                Name = c.User?.Name ?? string.Empty,
                Phone = c.User?.Phone,
                TotalBookings = c.TotalBookings,
                TotalSpent = c.TotalSpent,
                LastBookingAt = c.LastBookingAt,
                Notes = c.Notes,
                IsBlocked = c.IsBlocked,
                CreatedAt = c.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}

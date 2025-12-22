using FreeStays.Application.DTOs.Customers;
using FreeStays.Application.Features.Customers.Extensions;
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
            Items = items.Select(c => c.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}

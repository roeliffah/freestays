using FreeStays.Domain.Entities;

namespace FreeStays.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? isBlocked = null,
        CancellationToken cancellationToken = default);
    Task<Customer?> GetWithBookingsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCustomerRoleCountAsync(CancellationToken cancellationToken = default);
}

using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Admin.Queries;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public record DashboardStatsDto
{
    public int TotalUsers { get; init; }
    public int TotalBookings { get; init; }
    public decimal TotalRevenue { get; init; }
    public int PendingBookings { get; init; }
    public int TodayBookings { get; init; }
    public decimal MonthlyRevenue { get; init; }
}

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetDashboardStatsQueryHandler(
        IBookingRepository bookingRepository,
        ICustomerRepository customerRepository)
    {
        _bookingRepository = bookingRepository;
        _customerRepository = customerRepository;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var allBookings = (await _bookingRepository.GetAllAsync(cancellationToken)).ToList();
        var allCustomers = (await _customerRepository.GetAllAsync(cancellationToken)).ToList();

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);

        var totalRevenue = allBookings.Sum(b => b.TotalPrice);
        var pendingBookings = allBookings.Count(b => b.Status == Domain.Enums.BookingStatus.Pending);
        var todayBookings = allBookings.Count(b => b.CreatedAt.Date == now.Date);
        var monthlyRevenue = allBookings
            .Where(b => b.CreatedAt >= currentMonthStart && b.CreatedAt <= now)
            .Sum(b => b.TotalPrice);

        return new DashboardStatsDto
        {
            TotalUsers = allCustomers.Count,
            TotalBookings = allBookings.Count,
            TotalRevenue = totalRevenue,
            PendingBookings = pendingBookings,
            TodayBookings = todayBookings,
            MonthlyRevenue = monthlyRevenue
        };
    }
}

using FreeStays.Application.DTOs.Admin;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Admin.Queries;

public record GetDashboardQuery : IRequest<DashboardResponse>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetDashboardQueryHandler(
        IBookingRepository bookingRepository,
        ICustomerRepository customerRepository)
    {
        _bookingRepository = bookingRepository;
        _customerRepository = customerRepository;
    }

    public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var stats = await GetDashboardStatsAsync(cancellationToken);
        var recentBookings = await GetRecentBookingsAsync(cancellationToken);
        var topDestinations = await GetTopDestinationsAsync(cancellationToken);

        return new DashboardResponse
        {
            Stats = stats,
            RecentBookings = recentBookings,
            TopDestinations = topDestinations
        };
    }

    private async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var lastMonth = now.AddMonths(-1);
        var previousMonth = now.AddMonths(-2);

        var allBookings = (await _bookingRepository.GetAllAsync(cancellationToken)).ToList();

        var currentBookings = allBookings.Where(b => b.CreatedAt >= lastMonth && b.CreatedAt <= now).ToList();
        var previousBookings = allBookings.Where(b => b.CreatedAt >= previousMonth && b.CreatedAt < lastMonth).ToList();

        var totalBookings = currentBookings.Count;
        var totalRevenue = currentBookings.Sum(b => b.TotalPrice);
        // Sadece Customer rollü kullanıcıları say
        var totalCustomers = await _customerRepository.GetCustomerRoleCountAsync(cancellationToken);
        var commission = totalRevenue * 0.10m; // %10 komisyon

        var bookingsGrowth = previousBookings.Count > 0
            ? ((double)(totalBookings - previousBookings.Count) / previousBookings.Count) * 100
            : 0;

        var prevRevenue = previousBookings.Sum(b => b.TotalPrice);
        var revenueGrowth = prevRevenue > 0
            ? ((double)(totalRevenue - prevRevenue) / (double)prevRevenue) * 100
            : 0;

        return new DashboardStats
        {
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            TotalCustomers = totalCustomers,
            Commission = commission,
            BookingsGrowth = Math.Round(bookingsGrowth, 1),
            RevenueGrowth = Math.Round(revenueGrowth, 1)
        };
    }

    private async Task<List<RecentBooking>> GetRecentBookingsAsync(CancellationToken cancellationToken)
    {
        var bookings = (await _bookingRepository.GetAllAsync(cancellationToken))
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToList();

        var recentBookings = new List<RecentBooking>();

        foreach (var booking in bookings)
        {
            // User bilgisini al
            var user = await _customerRepository.GetByIdAsync(booking.UserId, cancellationToken);

            // Hotel/Service adını belirle
            string serviceName = booking.Type switch
            {
                BookingType.Hotel => booking.HotelBooking?.Hotel?.Name ?? "Hotel",
                BookingType.Flight => booking.FlightBooking?.Airline ?? "Flight",
                BookingType.Car => booking.CarRental?.CarModel ?? "Car Rental",
                _ => "Unknown"
            };

            recentBookings.Add(new RecentBooking
            {
                Id = booking.Id.ToString(),
                Customer = user?.User?.Name ?? "Unknown",
                Type = booking.Type.ToString().ToLower(),
                Hotel = serviceName,
                Amount = booking.TotalPrice,
                Status = booking.Status.ToString().ToLower(),
                Date = booking.CreatedAt
            });
        }

        return recentBookings;
    }

    private async Task<List<TopDestination>> GetTopDestinationsAsync(CancellationToken cancellationToken)
    {
        var allBookings = (await _bookingRepository.GetAllAsync(cancellationToken)).ToList();
        var totalBookings = allBookings.Count;

        // Hotel rezervasyonlarından şehir bilgilerini topla
        var destinations = allBookings
            .Where(b => b.Type == BookingType.Hotel && b.HotelBooking?.Hotel != null)
            .GroupBy(b => b.HotelBooking!.Hotel!.City)
            .Select(g => new
            {
                Name = g.Key,
                Bookings = g.Count()
            })
            .OrderByDescending(x => x.Bookings)
            .Take(5)
            .ToList();

        return destinations.Select(d => new TopDestination
        {
            Name = d.Name,
            Bookings = d.Bookings,
            Percent = totalBookings > 0 ? (int)((d.Bookings * 100.0) / totalBookings) : 0
        }).ToList();
    }
}

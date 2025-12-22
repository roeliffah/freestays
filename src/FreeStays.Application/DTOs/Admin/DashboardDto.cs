namespace FreeStays.Application.DTOs.Admin;

public record DashboardResponse
{
    public DashboardStats Stats { get; init; } = null!;
    public List<RecentBooking> RecentBookings { get; init; } = new();
    public List<TopDestination> TopDestinations { get; init; } = new();
}

public record DashboardStats
{
    public int TotalBookings { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalCustomers { get; init; } // Sadece Customer rollü kullanıcılar
    public decimal Commission { get; init; }
    public double BookingsGrowth { get; init; }
    public double RevenueGrowth { get; init; }
}

public record RecentBooking
{
    public string Id { get; init; } = string.Empty;
    public string Customer { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // "hotel", "flight", "car"
    public string Hotel { get; init; } = string.Empty; // veya Service
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty; // "confirmed", "pending", "cancelled"
    public DateTime Date { get; init; }
}

public record TopDestination
{
    public string Name { get; init; } = string.Empty;
    public int Bookings { get; init; }
    public int Percent { get; init; }
}

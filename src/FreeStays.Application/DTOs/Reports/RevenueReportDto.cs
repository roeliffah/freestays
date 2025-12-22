namespace FreeStays.Application.DTOs.Reports;

public record RevenueReportDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal HotelRevenue { get; init; }
    public decimal FlightRevenue { get; init; }
    public decimal CarRevenue { get; init; }
}
using FreeStays.Application.DTOs.Reports;
using FreeStays.Domain.Interfaces;
using MediatR;

namespace FreeStays.Application.Features.Admin.Queries;

public record GetRevenueReportQuery(DateTime From, DateTime To) : IRequest<RevenueReportDto>;

public class GetRevenueReportQueryHandler : IRequestHandler<GetRevenueReportQuery, RevenueReportDto>
{
    private readonly IBookingRepository _bookingRepository;

    public GetRevenueReportQueryHandler(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<RevenueReportDto> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken)
    {
        var bookings = (await _bookingRepository.GetAllAsync(cancellationToken))
            .Where(b => b.CreatedAt >= request.From && b.CreatedAt <= request.To)
            .ToList();

        var totalRevenue = bookings.Sum(b => b.TotalPrice);
        var hotelRevenue = bookings.Where(b => b.Type == Domain.Enums.BookingType.Hotel).Sum(b => b.TotalPrice);
        var flightRevenue = bookings.Where(b => b.Type == Domain.Enums.BookingType.Flight).Sum(b => b.TotalPrice);
        var carRevenue = bookings.Where(b => b.Type == Domain.Enums.BookingType.Car).Sum(b => b.TotalPrice);

        return new RevenueReportDto
        {
            FromDate = request.From,
            ToDate = request.To,
            TotalRevenue = totalRevenue,
            HotelRevenue = hotelRevenue,
            FlightRevenue = flightRevenue,
            CarRevenue = carRevenue
        };
    }
}

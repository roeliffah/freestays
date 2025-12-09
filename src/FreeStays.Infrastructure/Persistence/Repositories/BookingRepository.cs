using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(FreeStaysDbContext context) : base(context)
    {
    }
    
    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.UserId == userId)
            .Include(b => b.HotelBooking)
                .ThenInclude(hb => hb!.Hotel)
            .Include(b => b.FlightBooking)
            .Include(b => b.CarRental)
            .Include(b => b.Payment)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Booking?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.User)
            .Include(b => b.HotelBooking)
                .ThenInclude(hb => hb!.Hotel)
                    .ThenInclude(h => h.Images.OrderBy(i => i.Order).Take(1))
            .Include(b => b.FlightBooking)
            .Include(b => b.CarRental)
            .Include(b => b.Payment)
            .Include(b => b.Coupon)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Status == status)
            .Include(b => b.User)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Booking>> GetRecentBookingsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.User)
            .Include(b => b.HotelBooking)
                .ThenInclude(hb => hb!.Hotel)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

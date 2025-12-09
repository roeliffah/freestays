using FreeStays.Domain.Entities;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Repositories;

public class HotelRepository : Repository<Hotel>, IHotelRepository
{
    public HotelRepository(FreeStaysDbContext context) : base(context)
    {
    }
    
    public async Task<Hotel?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(h => h.ExternalId == externalId, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Hotel>> SearchAsync(
        string? city, 
        string? country, 
        int? minCategory, 
        decimal? maxPrice, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(h => h.IsActive);
        
        if (!string.IsNullOrEmpty(city))
            query = query.Where(h => h.City.ToLower().Contains(city.ToLower()));
        
        if (!string.IsNullOrEmpty(country))
            query = query.Where(h => h.Country.ToLower().Contains(country.ToLower()));
        
        if (minCategory.HasValue)
            query = query.Where(h => h.Category >= minCategory.Value);
        
        if (maxPrice.HasValue)
            query = query.Where(h => h.MinPrice <= maxPrice.Value);
        
        return await query
            .Include(h => h.Images.OrderBy(i => i.Order).Take(5))
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Hotel>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => h.IsActive && h.Category >= 4)
            .Include(h => h.Images.OrderBy(i => i.Order).Take(3))
            .OrderByDescending(h => h.Category)
            .ThenBy(h => h.MinPrice)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Hotel?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(h => h.Images.OrderBy(i => i.Order))
            .Include(h => h.Facilities)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }
}

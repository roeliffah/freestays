using FreeStays.Domain.Entities;
using FreeStays.Domain.Entities.Cache;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Persistence.Context;

public class FreeStaysDbContext : DbContext
{
    public FreeStaysDbContext(DbContextOptions<FreeStaysDbContext> options) : base(options)
    {
    }
    
    // Core Entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<HotelImage> HotelImages => Set<HotelImage>();
    public DbSet<HotelFacility> HotelFacilities => Set<HotelFacility>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<HotelBooking> HotelBookings => Set<HotelBooking>();
    public DbSet<FlightBooking> FlightBookings => Set<FlightBooking>();
    public DbSet<CarRental> CarRentals => Set<CarRental>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<ExternalServiceConfig> ExternalServiceConfigs => Set<ExternalServiceConfig>();
    
    // Admin & CMS Entities
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();
    public DbSet<StaticPageTranslation> StaticPageTranslations => Set<StaticPageTranslation>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<SeoSetting> SeoSettings => Set<SeoSetting>();
    public DbSet<PaymentSetting> PaymentSettings => Set<PaymentSetting>();
    
    // SunHotels Cache Entities
    public DbSet<SunHotelsDestinationCache> SunHotelsDestinations => Set<SunHotelsDestinationCache>();
    public DbSet<SunHotelsResortCache> SunHotelsResorts => Set<SunHotelsResortCache>();
    public DbSet<SunHotelsMealCache> SunHotelsMeals => Set<SunHotelsMealCache>();
    public DbSet<SunHotelsRoomTypeCache> SunHotelsRoomTypes => Set<SunHotelsRoomTypeCache>();
    public DbSet<SunHotelsFeatureCache> SunHotelsFeatures => Set<SunHotelsFeatureCache>();
    public DbSet<SunHotelsThemeCache> SunHotelsThemes => Set<SunHotelsThemeCache>();
    public DbSet<SunHotelsLanguageCache> SunHotelsLanguages => Set<SunHotelsLanguageCache>();
    public DbSet<SunHotelsTransferTypeCache> SunHotelsTransferTypes => Set<SunHotelsTransferTypeCache>();
    public DbSet<SunHotelsHotelCache> SunHotelsHotels => Set<SunHotelsHotelCache>();
    public DbSet<SunHotelsRoomCache> SunHotelsRooms => Set<SunHotelsRoomCache>();
    public DbSet<SunHotelsNoteTypeCache> SunHotelsNoteTypes => Set<SunHotelsNoteTypeCache>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FreeStaysDbContext).Assembly);
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}

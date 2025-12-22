using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Interfaces;
using FreeStays.Infrastructure.Caching;
using FreeStays.Infrastructure.ExternalServices.SunHotels;
using FreeStays.Infrastructure.ExternalServices.SunHotels.Models;
using FreeStays.Infrastructure.Persistence.Context;
using FreeStays.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FreeStays.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FreeStaysDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(FreeStaysDbContext).Assembly.FullName)));
        
        // Redis (Optional - falls back to InMemory cache if not available)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            try
            {
                var redis = ConnectionMultiplexer.Connect(redisConnection + ",abortConnect=false");
                services.AddSingleton<IConnectionMultiplexer>(redis);
                services.AddScoped<ICacheService, RedisCacheService>();
            }
            catch
            {
                // Redis connection failed, use InMemory cache
                services.AddMemoryCache();
                services.AddScoped<ICacheService, InMemoryCacheService>();
            }
        }
        else
        {
            // No Redis configured, use InMemory cache
            services.AddMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }
        
        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IDestinationRepository, DestinationRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        
        // New CMS Repositories
        services.AddScoped<ITranslationRepository, TranslationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IStaticPageRepository, StaticPageRepository>();
        services.AddScoped<IFaqRepository, FaqRepository>();
        services.AddScoped<IFeaturedHotelRepository, FeaturedHotelRepository>();
        services.AddScoped<IFeaturedDestinationRepository, FeaturedDestinationRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<ISeoSettingRepository, SeoSettingRepository>();
        services.AddScoped<IPaymentSettingRepository, PaymentSettingRepository>();
        
        // External Services
        services.Configure<SunHotelsConfig>(configuration.GetSection("ExternalServices:SunHotels"));
        services.AddHttpClient<ISunHotelsService, SunHotelsService>();
        services.AddScoped<ISunHotelsCacheService, SunHotelsCacheService>();
        
        // Background Jobs
        services.AddScoped<BackgroundJobs.SunHotelsStaticDataSyncJob>();
        
        return services;
    }
}

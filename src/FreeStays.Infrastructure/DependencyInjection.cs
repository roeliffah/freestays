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
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

namespace FreeStays.Infrastructure;

// Polly Context için logger extension
internal static class PollyContextExtensions
{
    private const string LoggerKey = "ILogger";

    public static Context WithLogger(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue(LoggerKey, out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FreeStaysDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(FreeStaysDbContext).Assembly.FullName)));

        // Memory Cache (Always add - used by SunHotelsCacheService)
        services.AddMemoryCache();

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
                services.AddScoped<ICacheService, InMemoryCacheService>();
            }
        }
        else
        {
            // No Redis configured, use InMemory cache
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }

        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IDestinationRepository, DestinationRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IReferralEarningRepository, ReferralEarningRepository>();

        // New CMS Repositories
        services.AddScoped<ITranslationRepository, TranslationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IStaticPageRepository, StaticPageRepository>();
        services.AddScoped<IFaqRepository, FaqRepository>();
        services.AddScoped<IFeaturedHotelRepository, FeaturedHotelRepository>();
        services.AddScoped<IFeaturedDestinationRepository, FeaturedDestinationRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailSettingRepository, EmailSettingRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<ISeoSettingRepository, SeoSettingRepository>();
        services.AddScoped<IPaymentSettingRepository, PaymentSettingRepository>();
        services.AddScoped<IExternalServiceConfigRepository, ExternalServiceConfigRepository>();

        // External Services - SunHotels Configuration
        services.Configure<SunHotelsConfig>(configuration.GetSection("SunHotels"));

        // ✅ Polly Retry Policy: SunHotels API için 
        // 1. Transient HTTP errors (5xx, timeout) için 3 retry
        // 2. Exponential backoff: 2s, 4s, 8s
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx, 408 timeout
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429 Rate Limit
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning("SunHotels API call failed. Waiting {Delay}s before retry {RetryCount}/3. Status: {StatusCode}",
                        timespan.TotalSeconds, retryCount, outcome.Result?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
                });

        // Timeout policy: ensure static calls are not bound to short-lived request tokens
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(90));

        services.AddHttpClient<ISunHotelsService, SunHotelsService>()
            .ConfigureHttpClient(client =>
            {
                // ✅ FIX: 60 saniye timeout (Python httpx.Timeout(60.0) standardı)
                // SunHotels SearchV3 yavaş olabilir ama 60s makul bir limit
                client.Timeout = TimeSpan.FromSeconds(60);
            })
            .AddPolicyHandler(timeoutPolicy)
            .AddPolicyHandler(retryPolicy); // ✅ Polly retry policy ekle

        services.AddScoped<ISunHotelsCacheService, SunHotelsCacheService>();

        // ✅ SunHotels Redis Cache Layer (opsiyonel - DB cache'in üstünde hız katmanı)
        services.AddScoped<SunHotelsRedisCacheService>();

        // Popular destination cache warmup
        services.AddScoped<IPopularDestinationWarmupService, Services.PopularDestinationWarmupService>();

        // Media Service
        services.AddScoped<IMediaService, Services.MediaService>();

        // Email Service
        services.AddScoped<IEmailService, Services.EmailService>();

        // Stripe Payment Service
        services.AddScoped<IStripePaymentService, Services.StripePaymentService>();

        // Background Jobs
        services.AddScoped<BackgroundJobs.SunHotelsStaticDataSyncJob>();
        services.AddScoped<BackgroundJobs.PopularDestinationWarmupJob>();

        // ✅ Hangfire Job Monitoring - Email notifications (SMTP from database)
        services.AddScoped<BackgroundJobs.IHangfireJobMonitoringService, BackgroundJobs.HangfireJobMonitoringService>();

        return services;
    }
}

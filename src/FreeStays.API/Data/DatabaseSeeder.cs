using FreeStays.Application.Common.Interfaces;
using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FreeStaysDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FreeStaysDbContext>>();

        try
        {
            // Check database connectivity first
            logger.LogInformation("Checking database connectivity...");
            if (!await context.Database.CanConnectAsync())
            {
                logger.LogWarning("Database is not available. Skipping seeding.");
                return;
            }
            
            // Ensure database and schema exists
            logger.LogInformation("Ensuring database schema exists...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database is ready.");
            
            // Seed Admin User
            await SeedAdminUserAsync(context, passwordHasher, logger);
            
            // Seed Email Templates
            await SeedEmailTemplatesAsync(context, logger);
            
            // Seed Site Settings
            await SeedSiteSettingsAsync(context, logger);
            
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database. Application will continue.");
            // Don't throw - allow application to start even if seeding fails
        }
    }

    private static async Task SeedAdminUserAsync(
        FreeStaysDbContext context, 
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        const string adminEmail = "bilgi@teybilisim.com";
        
        if (await context.Users.AnyAsync(u => u.Email == adminEmail))
        {
            logger.LogInformation("Admin user already exists, skipping...");
            return;
        }

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash("20110303Tey48!."),
            Name = "Halit YILMAZ",
            Phone = "+905358959397",
            Locale = "tr",
            Role = UserRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(adminUser);
        
        // Also create a Customer record for the admin
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            TotalBookings = 0,
            TotalSpent = 0,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };
        
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Admin user created: {Email}", adminEmail);
    }

    private static async Task SeedEmailTemplatesAsync(FreeStaysDbContext context, ILogger logger)
    {
        if (await context.EmailTemplates.AnyAsync())
        {
            logger.LogInformation("Email templates already exist, skipping...");
            return;
        }

        var templates = new List<EmailTemplate>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "welcome",
                Subject = "{\"tr\":\"FreeStays'e Hoş Geldiniz!\",\"en\":\"Welcome to FreeStays!\"}",
                Body = "{\"tr\":\"<p>Sayın {{userName}},</p><p>FreeStays'e hoş geldiniz!</p>\",\"en\":\"<p>Dear {{userName}},</p><p>Welcome to FreeStays!</p>\"}",
                Variables = "[\"userName\",\"userEmail\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "booking_confirmation",
                Subject = "{\"tr\":\"Rezervasyon Onayı - #{{bookingId}}\",\"en\":\"Booking Confirmation - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<p>Sayın {{userName}},</p><p>Rezervasyonunuz onaylandı.</p><p>Otel: {{hotelName}}</p><p>Giriş: {{checkIn}}</p><p>Çıkış: {{checkOut}}</p>\",\"en\":\"<p>Dear {{userName}},</p><p>Your booking has been confirmed.</p><p>Hotel: {{hotelName}}</p><p>Check-in: {{checkIn}}</p><p>Check-out: {{checkOut}}</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"hotelName\",\"checkIn\",\"checkOut\",\"totalPrice\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "password_reset",
                Subject = "{\"tr\":\"Şifre Sıfırlama Talebi\",\"en\":\"Password Reset Request\"}",
                Body = "{\"tr\":\"<p>Sayın {{userName}},</p><p>Şifrenizi sıfırlamak için <a href='{{resetLink}}'>buraya tıklayın</a>.</p>\",\"en\":\"<p>Dear {{userName}},</p><p>Click <a href='{{resetLink}}'>here</a> to reset your password.</p>\"}",
                Variables = "[\"userName\",\"resetLink\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "booking_cancelled",
                Subject = "{\"tr\":\"Rezervasyon İptal Edildi - #{{bookingId}}\",\"en\":\"Booking Cancelled - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<p>Sayın {{userName}},</p><p>Rezervasyonunuz iptal edilmiştir.</p>\",\"en\":\"<p>Dear {{userName}},</p><p>Your booking has been cancelled.</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"hotelName\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "payment_received",
                Subject = "{\"tr\":\"Ödeme Alındı - #{{bookingId}}\",\"en\":\"Payment Received - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<p>Sayın {{userName}},</p><p>{{amount}} {{currency}} tutarındaki ödemeniz alınmıştır.</p>\",\"en\":\"<p>Dear {{userName}},</p><p>Your payment of {{amount}} {{currency}} has been received.</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"amount\",\"currency\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.EmailTemplates.AddRangeAsync(templates);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Email templates seeded: {Count} templates", templates.Count);
    }

    private static async Task SeedSiteSettingsAsync(FreeStaysDbContext context, ILogger logger)
    {
        if (await context.SiteSettings.AnyAsync())
        {
            logger.LogInformation("Site settings already exist, skipping...");
            return;
        }

        var settings = new List<SiteSetting>
        {
            new() { Id = Guid.NewGuid(), Key = "siteName", Value = "\"FreeStays\"", Group = "site", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "siteUrl", Value = "\"https://freestays.com\"", Group = "site", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "supportEmail", Value = "\"bilgi@teybilisim.com\"", Group = "site", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "supportPhone", Value = "\"+905358959397\"", Group = "site", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "defaultLocale", Value = "\"tr\"", Group = "site", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "defaultCurrency", Value = "\"TRY\"", Group = "site", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "maintenanceMode", Value = "false", Group = "site", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "defaultMetaTitle", Value = "\"FreeStays - En İyi Otel Fırsatları\"", Group = "seo", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Key = "defaultMetaDescription", Value = "\"FreeStays ile en uygun otel fırsatlarını keşfedin.\"", Group = "seo", CreatedAt = DateTime.UtcNow }
        };

        await context.SiteSettings.AddRangeAsync(settings);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Site settings seeded: {Count} settings", settings.Count);
    }
}

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
            // Hoş Geldin E-postası
            new()
            {
                Id = Guid.NewGuid(),
                Code = "welcome",
                Subject = "{\"tr\":\"FreeStays'e Hoş Geldiniz!\",\"en\":\"Welcome to FreeStays!\"}",
                Body = "{\"tr\":\"<h2>Hoş Geldiniz {{userName}}!</h2><p>FreeStays ailesine katıldığınız için teşekkür ederiz.</p><p>Hesabınız başarıyla oluşturuldu. Artık en uygun otel, uçak bileti ve araç kiralama fırsatlarından yararlanabilirsiniz.</p><p>İyi tatiller dileriz!</p>\",\"en\":\"<h2>Welcome {{userName}}!</h2><p>Thank you for joining the FreeStays family.</p><p>Your account has been created successfully. You can now take advantage of the best hotel, flight and car rental deals.</p><p>Happy travels!</p>\"}",
                Variables = "[\"userName\",\"userEmail\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            
            // Otel Rezervasyon Onayı
            new()
            {
                Id = Guid.NewGuid(),
                Code = "hotel_booking_confirmed",
                Subject = "{\"tr\":\"Otel Rezervasyonunuz Onaylandı - #{{bookingId}}\",\"en\":\"Hotel Booking Confirmed - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<h2>Rezervasyonunuz Onaylandı!</h2><p>Sayın {{userName}},</p><p>Otel rezervasyonunuz başarıyla tamamlanmıştır.</p><hr><p><strong>Rezervasyon Detayları:</strong></p><ul><li>Rezervasyon No: <strong>{{bookingId}}</strong></li><li>Otel: <strong>{{hotelName}}</strong></li><li>Giriş Tarihi: {{checkInDate}}</li><li>Çıkış Tarihi: {{checkOutDate}}</li><li>Misafir Sayısı: {{guestCount}}</li><li>Oda Tipi: {{roomType}}</li><li>Toplam Tutar: <strong>{{totalAmount}} {{currency}}</strong></li></ul><p>İyi tatiller dileriz!</p>\",\"en\":\"<h2>Booking Confirmed!</h2><p>Dear {{userName}},</p><p>Your hotel booking has been successfully completed.</p><hr><p><strong>Booking Details:</strong></p><ul><li>Booking ID: <strong>{{bookingId}}</strong></li><li>Hotel: <strong>{{hotelName}}</strong></li><li>Check-in: {{checkInDate}}</li><li>Check-out: {{checkOutDate}}</li><li>Guests: {{guestCount}}</li><li>Room Type: {{roomType}}</li><li>Total Amount: <strong>{{totalAmount}} {{currency}}</strong></li></ul><p>Happy travels!</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"hotelName\",\"checkInDate\",\"checkOutDate\",\"guestCount\",\"roomType\",\"totalAmount\",\"currency\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            
            // Uçak Bileti Rezervasyon Onayı
            new()
            {
                Id = Guid.NewGuid(),
                Code = "flight_booking_confirmed",
                Subject = "{\"tr\":\"Uçak Biletiniz Onaylandı - #{{bookingId}}\",\"en\":\"Flight Ticket Confirmed - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<h2>Uçak Biletiniz Hazır!</h2><p>Sayın {{userName}},</p><p>Uçak bileti rezervasyonunuz başarıyla tamamlanmıştır.</p><hr><p><strong>Uçuş Detayları:</strong></p><ul><li>Rezervasyon No: <strong>{{bookingId}}</strong></li><li>Havayolu: {{airlineName}}</li><li>Uçuş No: {{flightNumber}}</li><li>Kalkış: {{departureCity}} - {{departureDate}} {{departureTime}}</li><li>Varış: {{arrivalCity}} - {{arrivalDate}} {{arrivalTime}}</li><li>Yolcu Sayısı: {{passengerCount}}</li><li>Toplam Tutar: <strong>{{totalAmount}} {{currency}}</strong></li></ul><p>Lütfen uçuştan 2 saat önce havalimanında bulunun.</p><p>İyi uçuşlar!</p>\",\"en\":\"<h2>Flight Ticket Ready!</h2><p>Dear {{userName}},</p><p>Your flight booking has been successfully completed.</p><hr><p><strong>Flight Details:</strong></p><ul><li>Booking ID: <strong>{{bookingId}}</strong></li><li>Airline: {{airlineName}}</li><li>Flight Number: {{flightNumber}}</li><li>Departure: {{departureCity}} - {{departureDate}} {{departureTime}}</li><li>Arrival: {{arrivalCity}} - {{arrivalDate}} {{arrivalTime}}</li><li>Passengers: {{passengerCount}}</li><li>Total Amount: <strong>{{totalAmount}} {{currency}}</strong></li></ul><p>Please arrive at the airport 2 hours before departure.</p><p>Have a great flight!</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"airlineName\",\"flightNumber\",\"departureCity\",\"departureDate\",\"departureTime\",\"arrivalCity\",\"arrivalDate\",\"arrivalTime\",\"passengerCount\",\"totalAmount\",\"currency\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            
            // Araç Kiralama Onayı
            new()
            {
                Id = Guid.NewGuid(),
                Code = "car_rental_confirmed",
                Subject = "{\"tr\":\"Araç Kiralama Onayı - #{{bookingId}}\",\"en\":\"Car Rental Confirmed - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<h2>Araç Kiralama Onaylandı!</h2><p>Sayın {{userName}},</p><p>Araç kiralama rezervasyonunuz başarıyla tamamlanmıştır.</p><hr><p><strong>Rezervasyon Detayları:</strong></p><ul><li>Rezervasyon No: <strong>{{bookingId}}</strong></li><li>Araç: {{carModel}}</li><li>Teslim Alma: {{pickupLocation}} - {{pickupDate}}</li><li>Teslim Etme: {{dropoffLocation}} - {{dropoffDate}}</li><li>Günlük Ücret: {{dailyRate}} {{currency}}</li><li>Toplam Tutar: <strong>{{totalAmount}} {{currency}}</strong></li></ul><p>Lütfen teslim alırken ehliyetinizi ve kredi kartınızı yanınızda bulundurun.</p><p>Güvenli sürüşler!</p>\",\"en\":\"<h2>Car Rental Confirmed!</h2><p>Dear {{userName}},</p><p>Your car rental booking has been successfully completed.</p><hr><p><strong>Booking Details:</strong></p><ul><li>Booking ID: <strong>{{bookingId}}</strong></li><li>Car: {{carModel}}</li><li>Pickup: {{pickupLocation}} - {{pickupDate}}</li><li>Dropoff: {{dropoffLocation}} - {{dropoffDate}}</li><li>Daily Rate: {{dailyRate}} {{currency}}</li><li>Total Amount: <strong>{{totalAmount}} {{currency}}</strong></li></ul><p>Please bring your driver's license and credit card for pickup.</p><p>Safe travels!</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"carModel\",\"pickupLocation\",\"pickupDate\",\"dropoffLocation\",\"dropoffDate\",\"dailyRate\",\"totalAmount\",\"currency\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            
            // Ödeme Onayı
            new()
            {
                Id = Guid.NewGuid(),
                Code = "payment_confirmed",
                Subject = "{\"tr\":\"Ödemeniz Alındı - #{{bookingId}}\",\"en\":\"Payment Received - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<h2>Ödeme Başarılı!</h2><p>Sayın {{userName}},</p><p>{{amount}} {{currency}} tutarındaki ödemeniz başarıyla alınmıştır.</p><hr><p><strong>Ödeme Detayları:</strong></p><ul><li>Rezervasyon No: {{bookingId}}</li><li>Ödeme Tarihi: {{paymentDate}}</li><li>Tutar: <strong>{{amount}} {{currency}}</strong></li><li>Ödeme Yöntemi: {{paymentMethod}}</li></ul><p>Faturanız e-posta adresinize gönderilmiştir.</p><p>Teşekkür ederiz!</p>\",\"en\":\"<h2>Payment Successful!</h2><p>Dear {{userName}},</p><p>Your payment of {{amount}} {{currency}} has been successfully received.</p><hr><p><strong>Payment Details:</strong></p><ul><li>Booking ID: {{bookingId}}</li><li>Payment Date: {{paymentDate}}</li><li>Amount: <strong>{{amount}} {{currency}}</strong></li><li>Payment Method: {{paymentMethod}}</li></ul><p>Your invoice has been sent to your email address.</p><p>Thank you!</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"amount\",\"currency\",\"paymentDate\",\"paymentMethod\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            
            // Rezervasyon İptali
            new()
            {
                Id = Guid.NewGuid(),
                Code = "booking_cancelled",
                Subject = "{\"tr\":\"Rezervasyon İptal Edildi - #{{bookingId}}\",\"en\":\"Booking Cancelled - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<h2>Rezervasyon İptali</h2><p>Sayın {{userName}},</p><p>Rezervasyonunuz iptal edilmiştir.</p><hr><p><strong>İptal Edilen Rezervasyon:</strong></p><ul><li>Rezervasyon No: {{bookingId}}</li><li>Hizmet: {{serviceName}}</li><li>İptal Tarihi: {{cancellationDate}}</li><li>İade Tutarı: {{refundAmount}} {{currency}}</li></ul><p>İade işlemi 5-10 iş günü içerisinde hesabınıza yansıyacaktır.</p>\",\"en\":\"<h2>Booking Cancellation</h2><p>Dear {{userName}},</p><p>Your booking has been cancelled.</p><hr><p><strong>Cancelled Booking:</strong></p><ul><li>Booking ID: {{bookingId}}</li><li>Service: {{serviceName}}</li><li>Cancellation Date: {{cancellationDate}}</li><li>Refund Amount: {{refundAmount}} {{currency}}</li></ul><p>The refund will be processed to your account within 5-10 business days.</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"serviceName\",\"cancellationDate\",\"refundAmount\",\"currency\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            
            // Şifre Sıfırlama
            new()
            {
                Id = Guid.NewGuid(),
                Code = "password_reset",
                Subject = "{\"tr\":\"Şifre Sıfırlama Talebi\",\"en\":\"Password Reset Request\"}",
                Body = "{\"tr\":\"<h2>Şifre Sıfırlama</h2><p>Sayın {{userName}},</p><p>Hesabınız için şifre sıfırlama talebi aldık.</p><p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın:</p><p><a href='{{resetLink}}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Şifremi Sıfırla</a></p><p>Bu link 1 saat boyunca geçerlidir.</p><p><small>Eğer bu talebi siz yapmadıysanız, bu e-postayı dikkate almayın.</small></p>\",\"en\":\"<h2>Password Reset</h2><p>Dear {{userName}},</p><p>We received a password reset request for your account.</p><p>Click the link below to reset your password:</p><p><a href='{{resetLink}}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a></p><p>This link is valid for 1 hour.</p><p><small>If you didn't request this, please ignore this email.</small></p>\"}",
                Variables = "[\"userName\",\"resetLink\",\"expiryTime\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            
            // Rezervasyon Hatırlatıcı
            new()
            {
                Id = Guid.NewGuid(),
                Code = "booking_reminder",
                Subject = "{\"tr\":\"Rezervasyon Hatırlatması - #{{bookingId}}\",\"en\":\"Booking Reminder - #{{bookingId}}\"}",
                Body = "{\"tr\":\"<h2>Yaklaşan Rezervasyonunuz</h2><p>Sayın {{userName}},</p><p>{{serviceName}} rezervasyonunuz yaklaşıyor!</p><hr><p><strong>Hatırlatma:</strong></p><ul><li>Rezervasyon No: {{bookingId}}</li><li>Tarih: {{date}}</li><li>Hizmet: {{serviceName}}</li></ul><p>İyi yolculuklar dileriz!</p>\",\"en\":\"<h2>Upcoming Booking</h2><p>Dear {{userName}},</p><p>Your {{serviceName}} booking is coming up!</p><hr><p><strong>Reminder:</strong></p><ul><li>Booking ID: {{bookingId}}</li><li>Date: {{date}}</li><li>Service: {{serviceName}}</li></ul><p>Have a great trip!</p>\"}",
                Variables = "[\"userName\",\"bookingId\",\"serviceName\",\"date\"]",
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

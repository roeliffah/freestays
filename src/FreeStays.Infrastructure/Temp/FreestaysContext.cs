using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.Infrastructure.Temp;

public partial class FreestaysContext : DbContext
{
    public FreestaysContext()
    {
    }

    public FreestaysContext(DbContextOptions<FreestaysContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<CarRental> CarRentals { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Destination> Destinations { get; set; }

    public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }

    public virtual DbSet<ExternalServiceConfig> ExternalServiceConfigs { get; set; }

    public virtual DbSet<Faq> Faqs { get; set; }

    public virtual DbSet<FaqTranslation> FaqTranslations { get; set; }

    public virtual DbSet<FeaturedDestination> FeaturedDestinations { get; set; }

    public virtual DbSet<FeaturedHotel> FeaturedHotels { get; set; }

    public virtual DbSet<FlightBooking> FlightBookings { get; set; }

    public virtual DbSet<Hotel> Hotels { get; set; }

    public virtual DbSet<HotelBooking> HotelBookings { get; set; }

    public virtual DbSet<HotelFacility> HotelFacilities { get; set; }

    public virtual DbSet<HotelImage> HotelImages { get; set; }

    public virtual DbSet<JobHistory> JobHistories { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentSetting> PaymentSettings { get; set; }

    public virtual DbSet<SeoSetting> SeoSettings { get; set; }

    public virtual DbSet<SiteSetting> SiteSettings { get; set; }

    public virtual DbSet<StaticPage> StaticPages { get; set; }

    public virtual DbSet<StaticPageTranslation> StaticPageTranslations { get; set; }

    public virtual DbSet<SunhotelsDestinationsCache> SunhotelsDestinationsCaches { get; set; }

    public virtual DbSet<SunhotelsFeaturesCache> SunhotelsFeaturesCaches { get; set; }

    public virtual DbSet<SunhotelsHotelsCache> SunhotelsHotelsCaches { get; set; }

    public virtual DbSet<SunhotelsLanguagesCache> SunhotelsLanguagesCaches { get; set; }

    public virtual DbSet<SunhotelsMealsCache> SunhotelsMealsCaches { get; set; }

    public virtual DbSet<SunhotelsNotetypesCache> SunhotelsNotetypesCaches { get; set; }

    public virtual DbSet<SunhotelsResortsCache> SunhotelsResortsCaches { get; set; }

    public virtual DbSet<SunhotelsRoomsCache> SunhotelsRoomsCaches { get; set; }

    public virtual DbSet<SunhotelsRoomtypesCache> SunhotelsRoomtypesCaches { get; set; }

    public virtual DbSet<SunhotelsThemesCache> SunhotelsThemesCaches { get; set; }

    public virtual DbSet<SunhotelsTransfertypesCache> SunhotelsTransfertypesCaches { get; set; }

    public virtual DbSet<Translation> Translations { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");

            entity.HasIndex(e => e.CouponId, "IX_bookings_coupon_id");

            entity.HasIndex(e => e.Status, "IX_bookings_status");

            entity.HasIndex(e => e.UserId, "IX_bookings_user_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Commission)
                .HasPrecision(18, 2)
                .HasColumnName("commission");
            entity.Property(e => e.CouponDiscount)
                .HasPrecision(18, 2)
                .HasColumnName("coupon_discount");
            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'EUR'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(18, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CarRental>(entity =>
        {
            entity.ToTable("car_rentals");

            entity.HasIndex(e => e.BookingId, "IX_car_rentals_booking_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CarModel)
                .HasMaxLength(200)
                .HasColumnName("car_model");
            entity.Property(e => e.CarType)
                .HasMaxLength(100)
                .HasColumnName("car_type");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.DriverLicense)
                .HasMaxLength(50)
                .HasColumnName("driver_license");
            entity.Property(e => e.DriverName)
                .HasMaxLength(200)
                .HasColumnName("driver_name");
            entity.Property(e => e.DropoffDate).HasColumnName("dropoff_date");
            entity.Property(e => e.DropoffLocation)
                .HasMaxLength(500)
                .HasColumnName("dropoff_location");
            entity.Property(e => e.ExternalBookingId)
                .HasMaxLength(100)
                .HasColumnName("external_booking_id");
            entity.Property(e => e.PickupDate).HasColumnName("pickup_date");
            entity.Property(e => e.PickupLocation)
                .HasMaxLength(500)
                .HasColumnName("pickup_location");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Booking).WithOne(p => p.CarRental).HasForeignKey<CarRental>(d => d.BookingId);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("coupons");

            entity.HasIndex(e => e.Code, "IX_coupons_code").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.DiscountType).HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasPrecision(18, 2)
                .HasColumnName("discount_value");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxUses).HasColumnName("max_uses");
            entity.Property(e => e.MinBookingAmount)
                .HasPrecision(18, 2)
                .HasColumnName("min_booking_amount");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UsedCount)
                .HasDefaultValue(0)
                .HasColumnName("used_count");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Customers_UserId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.TotalSpent).HasPrecision(18, 2);

            entity.HasOne(d => d.User).WithOne(p => p.Customer).HasForeignKey<Customer>(d => d.UserId);
        });

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.ToTable("destinations");

            entity.HasIndex(e => e.ExternalId, "IX_destinations_external_id").IsUnique();

            entity.HasIndex(e => e.Name, "IX_destinations_name");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(100)
                .HasColumnName("external_id");
            entity.Property(e => e.IsPopular)
                .HasDefaultValue(false)
                .HasColumnName("is_popular");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.SyncedAt).HasColumnName("synced_at");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_EmailTemplates_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Body).HasColumnType("jsonb");
            entity.Property(e => e.Code).HasMaxLength(100);
            entity.Property(e => e.Subject).HasColumnType("jsonb");
            entity.Property(e => e.Variables).HasColumnType("jsonb");
        });

        modelBuilder.Entity<ExternalServiceConfig>(entity =>
        {
            entity.ToTable("external_service_configs");

            entity.HasIndex(e => e.ServiceName, "IX_external_service_configs_service_name").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ApiKey)
                .HasMaxLength(500)
                .HasColumnName("api_key");
            entity.Property(e => e.ApiSecret)
                .HasMaxLength(500)
                .HasColumnName("api_secret");
            entity.Property(e => e.BaseUrl)
                .HasMaxLength(500)
                .HasColumnName("base_url");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Password)
                .HasMaxLength(500)
                .HasColumnName("password");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(100)
                .HasColumnName("service_name");
            entity.Property(e => e.Settings)
                .HasColumnType("jsonb")
                .HasColumnName("settings");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(200)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Faq>(entity =>
        {
            entity.HasIndex(e => e.Category, "IX_Faqs_Category");

            entity.HasIndex(e => e.Order, "IX_Faqs_Order");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<FaqTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.FaqId, e.Locale }, "IX_FaqTranslations_FaqId_Locale").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(10);
            entity.Property(e => e.Question).HasMaxLength(500);

            entity.HasOne(d => d.Faq).WithMany(p => p.FaqTranslations).HasForeignKey(d => d.FaqId);
        });

        modelBuilder.Entity<FeaturedDestination>(entity =>
        {
            entity.HasIndex(e => e.DestinationId, "IX_FeaturedDestinations_DestinationId");

            entity.HasIndex(e => e.Priority, "IX_FeaturedDestinations_Priority");

            entity.HasIndex(e => e.Season, "IX_FeaturedDestinations_Season");

            entity.HasIndex(e => e.Status, "IX_FeaturedDestinations_Status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.Property(e => e.DestinationId).HasMaxLength(50);
            entity.Property(e => e.DestinationName).HasMaxLength(200);
            entity.Property(e => e.Image).HasMaxLength(500);
            entity.Property(e => e.Priority).HasDefaultValue(999);
            entity.Property(e => e.Season).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<FeaturedHotel>(entity =>
        {
            entity.HasIndex(e => e.HotelId, "IX_FeaturedHotels_HotelId");

            entity.HasIndex(e => e.Priority, "IX_FeaturedHotels_Priority");

            entity.HasIndex(e => e.Season, "IX_FeaturedHotels_Season");

            entity.HasIndex(e => e.Status, "IX_FeaturedHotels_Status");

            entity.HasIndex(e => new { e.ValidFrom, e.ValidUntil }, "IX_FeaturedHotels_ValidFrom_ValidUntil");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CampaignName).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.Priority).HasDefaultValue(999);
            entity.Property(e => e.Season).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Hotel).WithMany(p => p.FeaturedHotels)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FlightBooking>(entity =>
        {
            entity.ToTable("flight_bookings");

            entity.HasIndex(e => e.BookingId, "IX_flight_bookings_booking_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Airline)
                .HasMaxLength(100)
                .HasColumnName("airline");
            entity.Property(e => e.Arrival)
                .HasMaxLength(200)
                .HasColumnName("arrival");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Class)
                .HasMaxLength(50)
                .HasColumnName("class");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Departure)
                .HasMaxLength(200)
                .HasColumnName("departure");
            entity.Property(e => e.DepartureDate).HasColumnName("departure_date");
            entity.Property(e => e.ExternalBookingId)
                .HasMaxLength(100)
                .HasColumnName("external_booking_id");
            entity.Property(e => e.FlightNumber)
                .HasMaxLength(50)
                .HasColumnName("flight_number");
            entity.Property(e => e.Passengers).HasColumnName("passengers");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Booking).WithOne(p => p.FlightBooking).HasForeignKey<FlightBooking>(d => d.BookingId);
        });

        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.ToTable("hotels");

            entity.HasIndex(e => e.City, "IX_hotels_city");

            entity.HasIndex(e => e.Country, "IX_hotels_country");

            entity.HasIndex(e => e.ExternalId, "IX_hotels_external_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.City)
                .HasMaxLength(200)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'EUR'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(100)
                .HasColumnName("external_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.MinPrice)
                .HasPrecision(18, 2)
                .HasColumnName("min_price");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.SyncedAt).HasColumnName("synced_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<HotelBooking>(entity =>
        {
            entity.ToTable("hotel_bookings");

            entity.HasIndex(e => e.BookingId, "IX_hotel_bookings_booking_id").IsUnique();

            entity.HasIndex(e => e.HotelId, "IX_hotel_bookings_hotel_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Adults).HasColumnName("adults");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CheckIn).HasColumnName("check_in");
            entity.Property(e => e.CheckOut).HasColumnName("check_out");
            entity.Property(e => e.Children).HasColumnName("children");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExternalBookingId)
                .HasMaxLength(100)
                .HasColumnName("external_booking_id");
            entity.Property(e => e.GuestEmail)
                .HasMaxLength(256)
                .HasColumnName("guest_email");
            entity.Property(e => e.GuestName)
                .HasMaxLength(200)
                .HasColumnName("guest_name");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.RoomTypeId)
                .HasMaxLength(100)
                .HasColumnName("room_type_id");
            entity.Property(e => e.RoomTypeName)
                .HasMaxLength(200)
                .HasColumnName("room_type_name");
            entity.Property(e => e.SpecialRequests).HasColumnName("special_requests");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Booking).WithOne(p => p.HotelBooking).HasForeignKey<HotelBooking>(d => d.BookingId);

            entity.HasOne(d => d.Hotel).WithMany(p => p.HotelBookings)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HotelFacility>(entity =>
        {
            entity.ToTable("hotel_facilities");

            entity.HasIndex(e => e.HotelId, "IX_hotel_facilities_hotel_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Hotel).WithMany(p => p.HotelFacilities).HasForeignKey(d => d.HotelId);
        });

        modelBuilder.Entity<HotelImage>(entity =>
        {
            entity.ToTable("hotel_images");

            entity.HasIndex(e => e.HotelId, "IX_hotel_images_hotel_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Caption)
                .HasMaxLength(500)
                .HasColumnName("caption");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Url)
                .HasMaxLength(1000)
                .HasColumnName("url");

            entity.HasOne(d => d.Hotel).WithMany(p => p.HotelImages).HasForeignKey(d => d.HotelId);
        });

        modelBuilder.Entity<JobHistory>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasIndex(e => e.BookingId, "IX_payments_booking_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'EUR'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.FailureReason)
                .HasMaxLength(500)
                .HasColumnName("failure_reason");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.StripePaymentId)
                .HasMaxLength(200)
                .HasColumnName("stripe_payment_id");
            entity.Property(e => e.StripePaymentIntentId)
                .HasMaxLength(200)
                .HasColumnName("stripe_payment_intent_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Booking).WithOne(p => p.Payment).HasForeignKey<Payment>(d => d.BookingId);
        });

        modelBuilder.Entity<PaymentSetting>(entity =>
        {
            entity.HasIndex(e => e.Provider, "IX_PaymentSettings_Provider").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.PublicKey).HasMaxLength(500);
            entity.Property(e => e.SecretKey).HasMaxLength(500);
            entity.Property(e => e.Settings).HasColumnType("jsonb");
            entity.Property(e => e.WebhookSecret).HasMaxLength(500);
        });

        modelBuilder.Entity<SeoSetting>(entity =>
        {
            entity.HasIndex(e => new { e.Locale, e.PageType }, "IX_SeoSettings_Locale_PageType").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(10);
            entity.Property(e => e.MetaDescription).HasMaxLength(500);
            entity.Property(e => e.MetaKeywords).HasMaxLength(500);
            entity.Property(e => e.MetaTitle).HasMaxLength(200);
            entity.Property(e => e.OgImage).HasMaxLength(500);
            entity.Property(e => e.PageType).HasMaxLength(100);
        });

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.HasIndex(e => new { e.Group, e.Key }, "IX_SiteSettings_Group_Key").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Group).HasMaxLength(50);
            entity.Property(e => e.Key).HasMaxLength(200);
            entity.Property(e => e.Value).HasColumnType("jsonb");
        });

        modelBuilder.Entity<StaticPage>(entity =>
        {
            entity.HasIndex(e => e.Slug, "IX_StaticPages_Slug").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Slug).HasMaxLength(200);
        });

        modelBuilder.Entity<StaticPageTranslation>(entity =>
        {
            entity.HasIndex(e => new { e.PageId, e.Locale }, "IX_StaticPageTranslations_PageId_Locale").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Locale).HasMaxLength(10);
            entity.Property(e => e.MetaDescription).HasMaxLength(500);
            entity.Property(e => e.MetaTitle).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(500);

            entity.HasOne(d => d.Page).WithMany(p => p.StaticPageTranslations).HasForeignKey(d => d.PageId);
        });

        modelBuilder.Entity<SunhotelsDestinationsCache>(entity =>
        {
            entity.ToTable("sunhotels_destinations_cache");

            entity.HasIndex(e => e.DestinationId, "IX_sunhotels_destinations_cache_DestinationId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasMaxLength(10);
            entity.Property(e => e.CountryId)
                .HasMaxLength(50)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.DestinationCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.DestinationId).HasMaxLength(50);
            entity.Property(e => e.TimeZone).HasMaxLength(50);
        });

        modelBuilder.Entity<SunhotelsFeaturesCache>(entity =>
        {
            entity.ToTable("sunhotels_features_cache");

            entity.HasIndex(e => e.FeatureId, "IX_sunhotels_features_cache_FeatureId");

            entity.HasIndex(e => new { e.FeatureId, e.Language }, "IX_sunhotels_features_cache_FeatureId_Language").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");
        });

        modelBuilder.Entity<SunhotelsHotelsCache>(entity =>
        {
            entity.ToTable("sunhotels_hotels_cache");

            entity.HasIndex(e => e.Category, "IX_sunhotels_hotels_cache_Category");

            entity.HasIndex(e => e.City, "IX_sunhotels_hotels_cache_City");

            entity.HasIndex(e => e.CountryCode, "IX_sunhotels_hotels_cache_CountryCode");

            entity.HasIndex(e => e.HotelId, "IX_sunhotels_hotels_cache_HotelId");

            entity.HasIndex(e => new { e.HotelId, e.Language }, "IX_sunhotels_hotels_cache_HotelId_Language").IsUnique();

            entity.HasIndex(e => e.ResortId, "IX_sunhotels_hotels_cache_ResortId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(255);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasMaxLength(10);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Fax).HasMaxLength(50);
            entity.Property(e => e.FeatureIds)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb");
            entity.Property(e => e.GiataCode).HasMaxLength(50);
            entity.Property(e => e.ImageUrls)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb");
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.ResortName).HasMaxLength(255);
            entity.Property(e => e.ThemeIds)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb");
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.ZipCode).HasMaxLength(20);
        });

        modelBuilder.Entity<SunhotelsLanguagesCache>(entity =>
        {
            entity.ToTable("sunhotels_languages_cache");

            entity.HasIndex(e => e.LanguageCode, "IX_sunhotels_languages_cache_LanguageCode").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.LanguageCode).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<SunhotelsMealsCache>(entity =>
        {
            entity.ToTable("sunhotels_meals_cache");

            entity.HasIndex(e => e.MealId, "IX_sunhotels_meals_cache_MealId");

            entity.HasIndex(e => new { e.MealId, e.Language }, "IX_sunhotels_meals_cache_MealId_Language").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Labels)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb");
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");
        });

        modelBuilder.Entity<SunhotelsNotetypesCache>(entity =>
        {
            entity.ToTable("sunhotels_notetypes_cache");

            entity.HasIndex(e => new { e.NoteTypeId, e.NoteCategory }, "IX_sunhotels_notetypes_cache_NoteTypeId_NoteCategory");

            entity.HasIndex(e => new { e.NoteTypeId, e.NoteCategory, e.Language }, "IX_sunhotels_notetypes_cache_NoteTypeId_NoteCategory_Language").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");
            entity.Property(e => e.NoteCategory).HasMaxLength(50);
        });

        modelBuilder.Entity<SunhotelsResortsCache>(entity =>
        {
            entity.ToTable("sunhotels_resorts_cache");

            entity.HasIndex(e => e.ResortId, "IX_sunhotels_resorts_cache_ResortId");

            entity.HasIndex(e => new { e.ResortId, e.Language }, "IX_sunhotels_resorts_cache_ResortId_Language").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CountryCode).HasMaxLength(10);
            entity.Property(e => e.CountryName)
                .HasMaxLength(100)
                .HasDefaultValueSql("''::character varying");
            entity.Property(e => e.DestinationId).HasMaxLength(50);
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");
        });

        modelBuilder.Entity<SunhotelsRoomsCache>(entity =>
        {
            entity.ToTable("sunhotels_rooms_cache");

            entity.HasIndex(e => e.HotelCacheId, "IX_sunhotels_rooms_cache_HotelCacheId");

            entity.HasIndex(e => e.HotelId, "IX_sunhotels_rooms_cache_HotelId");

            entity.HasIndex(e => e.RoomTypeId, "IX_sunhotels_rooms_cache_RoomTypeId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FeatureIds)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb");
            entity.Property(e => e.ImageUrls)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb");
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");

            entity.HasOne(d => d.HotelCache).WithMany(p => p.SunhotelsRoomsCaches).HasForeignKey(d => d.HotelCacheId);
        });

        modelBuilder.Entity<SunhotelsRoomtypesCache>(entity =>
        {
            entity.ToTable("sunhotels_roomtypes_cache");

            entity.HasIndex(e => e.RoomTypeId, "IX_sunhotels_roomtypes_cache_RoomTypeId");

            entity.HasIndex(e => new { e.RoomTypeId, e.Language }, "IX_sunhotels_roomtypes_cache_RoomTypeId_Language").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");
        });

        modelBuilder.Entity<SunhotelsThemesCache>(entity =>
        {
            entity.ToTable("sunhotels_themes_cache");

            entity.HasIndex(e => e.ThemeId, "IX_sunhotels_themes_cache_ThemeId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<SunhotelsTransfertypesCache>(entity =>
        {
            entity.ToTable("sunhotels_transfertypes_cache");

            entity.HasIndex(e => e.TransferTypeId, "IX_sunhotels_transfertypes_cache_TransferTypeId");

            entity.HasIndex(e => new { e.TransferTypeId, e.Language }, "IX_sunhotels_transfertypes_cache_TransferTypeId_Language").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying");
        });

        modelBuilder.Entity<Translation>(entity =>
        {
            entity.HasIndex(e => new { e.Locale, e.Namespace, e.Key }, "IX_Translations_Locale_Namespace_Key").IsUnique();

            entity.HasIndex(e => e.UpdatedBy, "IX_Translations_UpdatedBy");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Key).HasMaxLength(500);
            entity.Property(e => e.Locale).HasMaxLength(10);
            entity.Property(e => e.Namespace).HasMaxLength(100);

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.Translations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "IX_users_email").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Locale)
                .HasMaxLength(10)
                .HasDefaultValueSql("'en'::character varying")
                .HasColumnName("locale");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(500)
                .HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiryTime).HasColumnName("refresh_token_expiry_time");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

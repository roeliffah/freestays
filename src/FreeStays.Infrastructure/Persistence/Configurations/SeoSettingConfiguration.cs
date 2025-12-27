using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class SeoSettingConfiguration : IEntityTypeConfiguration<SeoSetting>
{
    public void Configure(EntityTypeBuilder<SeoSetting> builder)
    {
        builder.ToTable("SeoSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.PageType)
            .IsRequired()
            .HasMaxLength(100);

        // Basic Meta Tags
        builder.Property(s => s.MetaTitle)
            .HasMaxLength(200);

        builder.Property(s => s.MetaDescription)
            .HasMaxLength(500);

        builder.Property(s => s.MetaKeywords)
            .HasMaxLength(500);

        // Open Graph
        builder.Property(s => s.OgImage)
            .HasMaxLength(500);

        builder.Property(s => s.OgType)
            .HasMaxLength(50);

        builder.Property(s => s.OgUrl)
            .HasMaxLength(500);

        builder.Property(s => s.OgSiteName)
            .HasMaxLength(200);

        builder.Property(s => s.OgLocale)
            .HasMaxLength(10);

        // Twitter Card
        builder.Property(s => s.TwitterCard)
            .HasMaxLength(50);

        builder.Property(s => s.TwitterImage)
            .HasMaxLength(500);

        builder.Property(s => s.TwitterSite)
            .HasMaxLength(100);

        builder.Property(s => s.TwitterCreator)
            .HasMaxLength(100);

        // Organization Schema
        builder.Property(s => s.OrganizationName)
            .HasMaxLength(200);

        builder.Property(s => s.OrganizationUrl)
            .HasMaxLength(500);

        builder.Property(s => s.OrganizationLogo)
            .HasMaxLength(500);

        builder.Property(s => s.OrganizationDescription)
            .HasMaxLength(1000);

        builder.Property(s => s.OrganizationSocialProfiles)
            .HasColumnType("jsonb");

        // Website Schema
        builder.Property(s => s.WebsiteName)
            .HasMaxLength(200);

        builder.Property(s => s.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(s => s.WebsiteSearchActionTarget)
            .HasMaxLength(500);

        // Contact Info
        builder.Property(s => s.ContactPhone)
            .HasMaxLength(50);

        builder.Property(s => s.ContactEmail)
            .HasMaxLength(200);

        builder.Property(s => s.BusinessAddress)
            .HasColumnType("jsonb");

        // Hotel Schema
        builder.Property(s => s.HotelSchemaType)
            .HasMaxLength(50);

        builder.Property(s => s.HotelName)
            .HasMaxLength(200);

        builder.Property(s => s.HotelImage)
            .HasMaxLength(500);

        builder.Property(s => s.HotelAddress)
            .HasColumnType("jsonb");

        builder.Property(s => s.HotelTelephone)
            .HasMaxLength(50);

        builder.Property(s => s.HotelPriceRange)
            .HasMaxLength(50);

        builder.Property(s => s.HotelAggregateRating)
            .HasColumnType("jsonb");

        // Search and FAQ Schema
        builder.Property(s => s.SearchActionTarget)
            .HasMaxLength(500);

        // Custom Structured Data
        builder.Property(s => s.StructuredDataJson)
            .HasColumnType("jsonb");

        builder.HasIndex(s => new { s.Locale, s.PageType })
            .IsUnique();
    }
}

using FreeStays.Domain.Entities;
using FreeStays.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class FeaturedHotelConfiguration : IEntityTypeConfiguration<FeaturedHotel>
{
    public void Configure(EntityTypeBuilder<FeaturedHotel> builder)
    {
        builder.ToTable("FeaturedHotels");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.HotelId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.Priority)
            .IsRequired()
            .HasDefaultValue(999);

        builder.Property(f => f.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(f => f.Season)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(f => f.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(f => f.CampaignName)
            .HasMaxLength(200);

        builder.Property(f => f.DiscountPercentage)
            .HasPrecision(5, 2);

        builder.Property(f => f.CreatedBy)
            .HasMaxLength(100);

        builder.HasOne(f => f.Hotel)
            .WithMany()
            .HasForeignKey(f => f.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.Priority);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.Season);
        builder.HasIndex(f => new { f.ValidFrom, f.ValidUntil });
        builder.HasIndex(f => f.HotelId);
    }
}

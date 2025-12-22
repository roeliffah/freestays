using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class FeaturedDestinationConfiguration : IEntityTypeConfiguration<FeaturedDestination>
{
    public void Configure(EntityTypeBuilder<FeaturedDestination> builder)
    {
        builder.ToTable("FeaturedDestinations");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.DestinationId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.DestinationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(f => f.Country)
            .IsRequired()
            .HasMaxLength(100);

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

        builder.Property(f => f.Image)
            .HasMaxLength(500);

        builder.HasIndex(f => f.Priority);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.Season);
        builder.HasIndex(f => f.DestinationId);
    }
}

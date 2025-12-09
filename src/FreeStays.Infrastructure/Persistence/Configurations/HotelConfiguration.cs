using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.ToTable("hotels");
        
        builder.HasKey(h => h.Id);
        
        builder.Property(h => h.Id)
            .HasColumnName("id");
        
        builder.Property(h => h.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(h => h.Name)
            .HasColumnName("name")
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(h => h.Description)
            .HasColumnName("description")
            .HasColumnType("text");
        
        builder.Property(h => h.Address)
            .HasColumnName("address")
            .HasMaxLength(500);
        
        builder.Property(h => h.City)
            .HasColumnName("city")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(h => h.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(h => h.Category)
            .HasColumnName("category");
        
        builder.Property(h => h.Latitude)
            .HasColumnName("latitude");
        
        builder.Property(h => h.Longitude)
            .HasColumnName("longitude");
        
        builder.Property(h => h.MinPrice)
            .HasColumnName("min_price")
            .HasColumnType("decimal(18,2)");
        
        builder.Property(h => h.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("EUR");
        
        builder.Property(h => h.SyncedAt)
            .HasColumnName("synced_at");
        
        builder.Property(h => h.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
        
        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasIndex(h => h.ExternalId).IsUnique();
        builder.HasIndex(h => h.City);
        builder.HasIndex(h => h.Country);
        
        builder.HasMany(h => h.Images)
            .WithOne(i => i.Hotel)
            .HasForeignKey(i => i.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(h => h.Facilities)
            .WithOne(f => f.Hotel)
            .HasForeignKey(f => f.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

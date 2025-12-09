using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class HotelFacilityConfiguration : IEntityTypeConfiguration<HotelFacility>
{
    public void Configure(EntityTypeBuilder<HotelFacility> builder)
    {
        builder.ToTable("hotel_facilities");
        
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.Id)
            .HasColumnName("id");
        
        builder.Property(f => f.HotelId)
            .HasColumnName("hotel_id");
        
        builder.Property(f => f.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(f => f.Category)
            .HasColumnName("category")
            .HasMaxLength(100);
        
        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at");
    }
}

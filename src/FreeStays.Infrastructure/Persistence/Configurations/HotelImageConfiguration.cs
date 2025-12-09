using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class HotelImageConfiguration : IEntityTypeConfiguration<HotelImage>
{
    public void Configure(EntityTypeBuilder<HotelImage> builder)
    {
        builder.ToTable("hotel_images");
        
        builder.HasKey(i => i.Id);
        
        builder.Property(i => i.Id)
            .HasColumnName("id");
        
        builder.Property(i => i.HotelId)
            .HasColumnName("hotel_id");
        
        builder.Property(i => i.Url)
            .HasColumnName("url")
            .HasMaxLength(1000)
            .IsRequired();
        
        builder.Property(i => i.Order)
            .HasColumnName("order");
        
        builder.Property(i => i.Caption)
            .HasColumnName("caption")
            .HasMaxLength(500);
        
        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at");
    }
}

using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class DestinationConfiguration : IEntityTypeConfiguration<Destination>
{
    public void Configure(EntityTypeBuilder<Destination> builder)
    {
        builder.ToTable("destinations");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Id)
            .HasColumnName("id");
        
        builder.Property(d => d.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(d => d.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(d => d.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(d => d.Type)
            .HasColumnName("type")
            .HasConversion<int>();
        
        builder.Property(d => d.IsPopular)
            .HasColumnName("is_popular")
            .HasDefaultValue(false);
        
        builder.Property(d => d.SyncedAt)
            .HasColumnName("synced_at");
        
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasIndex(d => d.ExternalId).IsUnique();
        builder.HasIndex(d => d.Name);
    }
}

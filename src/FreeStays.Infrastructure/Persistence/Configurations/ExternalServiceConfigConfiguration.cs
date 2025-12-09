using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class ExternalServiceConfigConfiguration : IEntityTypeConfiguration<ExternalServiceConfig>
{
    public void Configure(EntityTypeBuilder<ExternalServiceConfig> builder)
    {
        builder.ToTable("external_service_configs");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id");
        
        builder.Property(e => e.ServiceName)
            .HasColumnName("service_name")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(e => e.BaseUrl)
            .HasColumnName("base_url")
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(e => e.ApiKey)
            .HasColumnName("api_key")
            .HasMaxLength(500);
        
        builder.Property(e => e.ApiSecret)
            .HasColumnName("api_secret")
            .HasMaxLength(500);
        
        builder.Property(e => e.Username)
            .HasColumnName("username")
            .HasMaxLength(200);
        
        builder.Property(e => e.Password)
            .HasColumnName("password")
            .HasMaxLength(500);
        
        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
        
        builder.Property(e => e.Settings)
            .HasColumnName("settings")
            .HasColumnType("jsonb");
        
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasIndex(e => e.ServiceName).IsUnique();
    }
}

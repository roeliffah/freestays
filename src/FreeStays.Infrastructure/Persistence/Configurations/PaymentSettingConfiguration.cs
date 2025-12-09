using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class PaymentSettingConfiguration : IEntityTypeConfiguration<PaymentSetting>
{
    public void Configure(EntityTypeBuilder<PaymentSetting> builder)
    {
        builder.ToTable("PaymentSettings");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.PublicKey)
            .HasMaxLength(500);

        builder.Property(p => p.SecretKey)
            .HasMaxLength(500);

        builder.Property(p => p.WebhookSecret)
            .HasMaxLength(500);

        builder.Property(p => p.Settings)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasIndex(p => p.Provider)
            .IsUnique();
    }
}

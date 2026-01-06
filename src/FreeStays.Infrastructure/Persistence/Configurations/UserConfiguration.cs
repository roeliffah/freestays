using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20);

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<int>();

        builder.Property(u => u.Locale)
            .HasColumnName("locale")
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(u => u.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(500);

        builder.Property(u => u.RefreshTokenExpiryTime)
            .HasColumnName("refresh_token_expiry_time");

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Referral System
        builder.Property(u => u.ReferralCode)
            .HasColumnName("referral_code")
            .HasMaxLength(20);

        builder.Property(u => u.ReferredByUserId)
            .HasColumnName("referred_by_user_id");

        builder.HasOne(u => u.ReferredByUser)
            .WithMany(u => u.ReferredUsers)
            .HasForeignKey(u => u.ReferredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Billing Address
        builder.Property(u => u.BillingAddress)
            .HasColumnName("billing_address")
            .HasMaxLength(500);

        builder.Property(u => u.BillingCity)
            .HasColumnName("billing_city")
            .HasMaxLength(100);

        builder.Property(u => u.BillingCountry)
            .HasColumnName("billing_country")
            .HasMaxLength(100);

        builder.Property(u => u.BillingPostalCode)
            .HasColumnName("billing_postal_code")
            .HasMaxLength(20);

        builder.Property(u => u.BillingPhone)
            .HasColumnName("billing_phone")
            .HasMaxLength(20);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.ReferralCode).IsUnique();
    }
}

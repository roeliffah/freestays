using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Kind)
            .HasColumnName("kind")
            .HasConversion<int>();

        builder.Property(c => c.DiscountType)
            .HasColumnName("discount_type")
            .HasConversion<int>();

        builder.Property(c => c.DiscountValue)
            .HasColumnName("discount_value")
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.MaxUses)
            .HasColumnName("max_uses");

        builder.Property(c => c.UsedCount)
            .HasColumnName("used_count")
            .HasDefaultValue(0);

        builder.Property(c => c.MinBookingAmount)
            .HasColumnName("min_booking_amount")
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.ValidFrom)
            .HasColumnName("valid_from");

        builder.Property(c => c.ValidUntil)
            .HasColumnName("valid_until");

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.AssignedUserId)
            .HasColumnName("assigned_user_id");

        builder.Property(c => c.AssignedEmail)
            .HasColumnName("assigned_email")
            .HasMaxLength(256);

        builder.Property(c => c.UsedByUserId)
            .HasColumnName("used_by_user_id");

        builder.Property(c => c.UsedByEmail)
            .HasColumnName("used_by_email")
            .HasMaxLength(256);

        builder.Property(c => c.UsedAt)
            .HasColumnName("used_at");

        builder.Property(c => c.PriceAmount)
            .HasColumnName("price_amount")
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.PriceCurrency)
            .HasColumnName("price_currency")
            .HasMaxLength(10)
            .HasDefaultValue("EUR");

        builder.Property(c => c.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(c => c.Code).IsUnique();
    }
}

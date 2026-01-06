using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class ReferralEarningConfiguration : IEntityTypeConfiguration<ReferralEarning>
{
    public void Configure(EntityTypeBuilder<ReferralEarning> builder)
    {
        builder.ToTable("referral_earnings");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.ReferrerUserId)
            .HasColumnName("referrer_user_id")
            .IsRequired();

        builder.Property(r => r.ReferredUserId)
            .HasColumnName("referred_user_id")
            .IsRequired();

        builder.Property(r => r.BookingId)
            .HasColumnName("booking_id");

        builder.Property(r => r.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("EUR")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<int>();

        builder.Property(r => r.PaidAt)
            .HasColumnName("paid_at");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationships
        builder.HasOne(r => r.ReferrerUser)
            .WithMany(u => u.ReferralEarnings)
            .HasForeignKey(r => r.ReferrerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReferredUser)
            .WithMany()
            .HasForeignKey(r => r.ReferredUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(r => r.ReferrerUserId);
        builder.HasIndex(r => r.ReferredUserId);
        builder.HasIndex(r => r.BookingId);
        builder.HasIndex(r => r.Status);
    }
}

using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .HasColumnName("id");
        
        builder.Property(p => p.BookingId)
            .HasColumnName("booking_id");
        
        builder.Property(p => p.StripePaymentId)
            .HasColumnName("stripe_payment_id")
            .HasMaxLength(200);
        
        builder.Property(p => p.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);
        
        builder.Property(p => p.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)");
        
        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("EUR");
        
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<int>();
        
        builder.Property(p => p.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(500);
        
        builder.Property(p => p.PaidAt)
            .HasColumnName("paid_at");
        
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

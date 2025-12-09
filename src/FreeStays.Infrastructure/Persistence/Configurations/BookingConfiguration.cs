using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Id)
            .HasColumnName("id");
        
        builder.Property(b => b.UserId)
            .HasColumnName("user_id");
        
        builder.Property(b => b.Type)
            .HasColumnName("type")
            .HasConversion<int>();
        
        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<int>();
        
        builder.Property(b => b.TotalPrice)
            .HasColumnName("total_price")
            .HasColumnType("decimal(18,2)");
        
        builder.Property(b => b.Commission)
            .HasColumnName("commission")
            .HasColumnType("decimal(18,2)");
        
        builder.Property(b => b.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("EUR");
        
        builder.Property(b => b.CouponId)
            .HasColumnName("coupon_id");
        
        builder.Property(b => b.CouponDiscount)
            .HasColumnName("coupon_discount")
            .HasColumnType("decimal(18,2)");
        
        builder.Property(b => b.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");
        
        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.Status);
        
        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(b => b.Coupon)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CouponId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

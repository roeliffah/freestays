using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class HotelBookingConfiguration : IEntityTypeConfiguration<HotelBooking>
{
    public void Configure(EntityTypeBuilder<HotelBooking> builder)
    {
        builder.ToTable("hotel_bookings");
        
        builder.HasKey(hb => hb.Id);
        
        builder.Property(hb => hb.Id)
            .HasColumnName("id");
        
        builder.Property(hb => hb.BookingId)
            .HasColumnName("booking_id");
        
        builder.Property(hb => hb.HotelId)
            .HasColumnName("hotel_id");
        
        builder.Property(hb => hb.RoomTypeId)
            .HasColumnName("room_type_id")
            .HasMaxLength(100);
        
        builder.Property(hb => hb.RoomTypeName)
            .HasColumnName("room_type_name")
            .HasMaxLength(200);
        
        builder.Property(hb => hb.CheckIn)
            .HasColumnName("check_in");
        
        builder.Property(hb => hb.CheckOut)
            .HasColumnName("check_out");
        
        builder.Property(hb => hb.Adults)
            .HasColumnName("adults");
        
        builder.Property(hb => hb.Children)
            .HasColumnName("children");
        
        builder.Property(hb => hb.ExternalBookingId)
            .HasColumnName("external_booking_id")
            .HasMaxLength(100);
        
        builder.Property(hb => hb.GuestName)
            .HasColumnName("guest_name")
            .HasMaxLength(200);
        
        builder.Property(hb => hb.GuestEmail)
            .HasColumnName("guest_email")
            .HasMaxLength(256);
        
        builder.Property(hb => hb.SpecialRequests)
            .HasColumnName("special_requests")
            .HasColumnType("text");
        
        builder.Property(hb => hb.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(hb => hb.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasOne(hb => hb.Booking)
            .WithOne(b => b.HotelBooking)
            .HasForeignKey<HotelBooking>(hb => hb.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(hb => hb.Hotel)
            .WithMany(h => h.HotelBookings)
            .HasForeignKey(hb => hb.HotelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

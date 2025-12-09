using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class FlightBookingConfiguration : IEntityTypeConfiguration<FlightBooking>
{
    public void Configure(EntityTypeBuilder<FlightBooking> builder)
    {
        builder.ToTable("flight_bookings");
        
        builder.HasKey(fb => fb.Id);
        
        builder.Property(fb => fb.Id)
            .HasColumnName("id");
        
        builder.Property(fb => fb.BookingId)
            .HasColumnName("booking_id");
        
        builder.Property(fb => fb.FlightNumber)
            .HasColumnName("flight_number")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(fb => fb.Departure)
            .HasColumnName("departure")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(fb => fb.Arrival)
            .HasColumnName("arrival")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(fb => fb.DepartureDate)
            .HasColumnName("departure_date");
        
        builder.Property(fb => fb.ReturnDate)
            .HasColumnName("return_date");
        
        builder.Property(fb => fb.Passengers)
            .HasColumnName("passengers");
        
        builder.Property(fb => fb.ExternalBookingId)
            .HasColumnName("external_booking_id")
            .HasMaxLength(100);
        
        builder.Property(fb => fb.Airline)
            .HasColumnName("airline")
            .HasMaxLength(100);
        
        builder.Property(fb => fb.Class)
            .HasColumnName("class")
            .HasMaxLength(50);
        
        builder.Property(fb => fb.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(fb => fb.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasOne(fb => fb.Booking)
            .WithOne(b => b.FlightBooking)
            .HasForeignKey<FlightBooking>(fb => fb.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

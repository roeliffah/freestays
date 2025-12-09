using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class CarRentalConfiguration : IEntityTypeConfiguration<CarRental>
{
    public void Configure(EntityTypeBuilder<CarRental> builder)
    {
        builder.ToTable("car_rentals");
        
        builder.HasKey(cr => cr.Id);
        
        builder.Property(cr => cr.Id)
            .HasColumnName("id");
        
        builder.Property(cr => cr.BookingId)
            .HasColumnName("booking_id");
        
        builder.Property(cr => cr.CarType)
            .HasColumnName("car_type")
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(cr => cr.CarModel)
            .HasColumnName("car_model")
            .HasMaxLength(200);
        
        builder.Property(cr => cr.PickupLocation)
            .HasColumnName("pickup_location")
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(cr => cr.DropoffLocation)
            .HasColumnName("dropoff_location")
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(cr => cr.PickupDate)
            .HasColumnName("pickup_date");
        
        builder.Property(cr => cr.DropoffDate)
            .HasColumnName("dropoff_date");
        
        builder.Property(cr => cr.ExternalBookingId)
            .HasColumnName("external_booking_id")
            .HasMaxLength(100);
        
        builder.Property(cr => cr.DriverName)
            .HasColumnName("driver_name")
            .HasMaxLength(200);
        
        builder.Property(cr => cr.DriverLicense)
            .HasColumnName("driver_license")
            .HasMaxLength(50);
        
        builder.Property(cr => cr.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(cr => cr.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasOne(cr => cr.Booking)
            .WithOne(b => b.CarRental)
            .HasForeignKey<CarRental>(cr => cr.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("Faqs");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Order)
            .IsRequired();

        builder.Property(f => f.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.Category)
            .HasMaxLength(100);

        builder.HasMany(f => f.Translations)
            .WithOne(t => t.Faq)
            .HasForeignKey(t => t.FaqId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.Order);
        builder.HasIndex(f => f.Category);
    }
}

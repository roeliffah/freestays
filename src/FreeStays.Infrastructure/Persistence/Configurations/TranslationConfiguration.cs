using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("Translations");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.Namespace)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Key)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Value)
            .IsRequired();

        builder.HasIndex(t => new { t.Locale, t.Namespace, t.Key })
            .IsUnique();

        builder.HasOne(t => t.UpdatedByUser)
            .WithMany()
            .HasForeignKey(t => t.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

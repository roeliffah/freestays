using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class FaqTranslationConfiguration : IEntityTypeConfiguration<FaqTranslation>
{
    public void Configure(EntityTypeBuilder<FaqTranslation> builder)
    {
        builder.ToTable("FaqTranslations");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.Question)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Answer)
            .IsRequired();

        builder.HasIndex(t => new { t.FaqId, t.Locale })
            .IsUnique();
    }
}

using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class StaticPageTranslationConfiguration : IEntityTypeConfiguration<StaticPageTranslation>
{
    public void Configure(EntityTypeBuilder<StaticPageTranslation> builder)
    {
        builder.ToTable("StaticPageTranslations");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Content)
            .IsRequired();

        builder.Property(t => t.MetaTitle)
            .HasMaxLength(200);

        builder.Property(t => t.MetaDescription)
            .HasMaxLength(500);

        builder.HasIndex(t => new { t.PageId, t.Locale })
            .IsUnique();
    }
}

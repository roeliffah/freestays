using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class SeoSettingConfiguration : IEntityTypeConfiguration<SeoSetting>
{
    public void Configure(EntityTypeBuilder<SeoSetting> builder)
    {
        builder.ToTable("SeoSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.PageType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.MetaTitle)
            .HasMaxLength(200);

        builder.Property(s => s.MetaDescription)
            .HasMaxLength(500);

        builder.Property(s => s.MetaKeywords)
            .HasMaxLength(500);

        builder.Property(s => s.OgImage)
            .HasMaxLength(500);

        builder.HasIndex(s => new { s.Locale, s.PageType })
            .IsUnique();
    }
}

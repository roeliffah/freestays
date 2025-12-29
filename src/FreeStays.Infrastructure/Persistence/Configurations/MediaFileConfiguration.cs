using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Filename)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.OriginalFilename)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(x => x.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.Folder)
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("general");

        builder.Property(x => x.AltText)
            .HasMaxLength(500);

        builder.Property(x => x.Tags)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(x => x.UploadedBy)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.Folder)
            .HasDatabaseName("idx_media_folder");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_media_created_at");

        builder.HasIndex(x => x.UploadedBy)
            .HasDatabaseName("idx_media_uploaded_by");

        // Foreign key
        builder.HasOne(x => x.Uploader)
            .WithMany()
            .HasForeignKey(x => x.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

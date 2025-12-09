using FreeStays.Domain.Entities.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class SunHotelsDestinationCacheConfiguration : IEntityTypeConfiguration<SunHotelsDestinationCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsDestinationCache> builder)
    {
        builder.ToTable("sunhotels_destinations_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.DestinationId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.CountryCode).HasMaxLength(10);
        builder.Property(x => x.DestinationCode).HasMaxLength(50);
        builder.Property(x => x.CountryId).HasMaxLength(50);
        builder.Property(x => x.TimeZone).HasMaxLength(50);

        builder.HasIndex(x => x.DestinationId);
        builder.HasIndex(x => x.DestinationId).IsUnique();
    }
}

public class SunHotelsResortCacheConfiguration : IEntityTypeConfiguration<SunHotelsResortCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsResortCache> builder)
    {
        builder.ToTable("sunhotels_resorts_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DestinationId).HasMaxLength(50);
        builder.Property(x => x.DestinationName).HasMaxLength(500);
        builder.Property(x => x.CountryCode).HasMaxLength(10);
        builder.Property(x => x.CountryName).HasMaxLength(100);
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => x.ResortId);
        builder.HasIndex(x => new { x.ResortId, x.Language }).IsUnique();
    }
}

public class SunHotelsMealCacheConfiguration : IEntityTypeConfiguration<SunHotelsMealCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsMealCache> builder)
    {
        builder.ToTable("sunhotels_meals_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.MealId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Labels).HasColumnType("jsonb");
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => x.MealId);
        builder.HasIndex(x => new { x.MealId, x.Language }).IsUnique();
    }
}

public class SunHotelsRoomTypeCacheConfiguration : IEntityTypeConfiguration<SunHotelsRoomTypeCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsRoomTypeCache> builder)
    {
        builder.ToTable("sunhotels_roomtypes_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoomTypeId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => x.RoomTypeId);
        builder.HasIndex(x => new { x.RoomTypeId, x.Language }).IsUnique();
    }
}

public class SunHotelsFeatureCacheConfiguration : IEntityTypeConfiguration<SunHotelsFeatureCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsFeatureCache> builder)
    {
        builder.ToTable("sunhotels_features_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.FeatureId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => x.FeatureId);
        builder.HasIndex(x => new { x.FeatureId, x.Language }).IsUnique();
    }
}

public class SunHotelsThemeCacheConfiguration : IEntityTypeConfiguration<SunHotelsThemeCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsThemeCache> builder)
    {
        builder.ToTable("sunhotels_themes_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.EnglishName).HasMaxLength(500);

        builder.HasIndex(x => x.ThemeId).IsUnique();
    }
}

public class SunHotelsLanguageCacheConfiguration : IEntityTypeConfiguration<SunHotelsLanguageCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsLanguageCache> builder)
    {
        builder.ToTable("sunhotels_languages_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.LanguageCode).IsUnique();
    }
}

public class SunHotelsTransferTypeCacheConfiguration : IEntityTypeConfiguration<SunHotelsTransferTypeCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsTransferTypeCache> builder)
    {
        builder.ToTable("sunhotels_transfertypes_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransferTypeId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => x.TransferTypeId);
        builder.HasIndex(x => new { x.TransferTypeId, x.Language }).IsUnique();
    }
}

public class SunHotelsNoteTypeCacheConfiguration : IEntityTypeConfiguration<SunHotelsNoteTypeCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsNoteTypeCache> builder)
    {
        builder.ToTable("sunhotels_notetypes_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.NoteTypeId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NoteCategory).HasMaxLength(50).IsRequired(); // Hotel veya Room
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => new { x.NoteTypeId, x.NoteCategory });
        builder.HasIndex(x => new { x.NoteTypeId, x.NoteCategory, x.Language }).IsUnique();
    }
}

public class SunHotelsHotelCacheConfiguration : IEntityTypeConfiguration<SunHotelsHotelCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsHotelCache> builder)
    {
        builder.ToTable("sunhotels_hotels_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.ZipCode).HasMaxLength(20);
        builder.Property(x => x.City).HasMaxLength(255);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.CountryCode).HasMaxLength(10);
        builder.Property(x => x.GiataCode).HasMaxLength(50);
        builder.Property(x => x.ResortName).HasMaxLength(255);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Fax).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(255);
        builder.Property(x => x.Website).HasMaxLength(500);
        builder.Property(x => x.FeatureIds).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.ThemeIds).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.ImageUrls).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => x.HotelId);
        builder.HasIndex(x => new { x.HotelId, x.Language }).IsUnique();
        builder.HasIndex(x => x.ResortId);
        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.CountryCode);
        builder.HasIndex(x => x.Category);

        builder.HasMany(x => x.Rooms)
            .WithOne(x => x.HotelCache)
            .HasForeignKey(x => x.HotelCacheId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SunHotelsRoomCacheConfiguration : IEntityTypeConfiguration<SunHotelsRoomCache>
{
    public void Configure(EntityTypeBuilder<SunHotelsRoomCache> builder)
    {
        builder.ToTable("sunhotels_rooms_cache");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.EnglishName).HasMaxLength(500);
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.FeatureIds).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.ImageUrls).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");

        builder.HasIndex(x => x.HotelId);
        builder.HasIndex(x => x.RoomTypeId);
        builder.HasIndex(x => x.HotelCacheId);
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSunHotelsCacheTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sunhotels_destinations_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_destinations_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_features_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_features_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_hotels_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HotelId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    GiataCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResortId = table.Column<int>(type: "integer", nullable: false),
                    ResortName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Fax = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FeatureIds = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    ThemeIds = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    ImageUrls = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_hotels_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_languages_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_languages_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_meals_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MealId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnglishLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_meals_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_notetypes_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NoteCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_notetypes_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_resorts_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResortId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DestinationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DestinationName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_resorts_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_roomtypes_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MaxOccupancy = table.Column<int>(type: "integer", nullable: false),
                    MinOccupancy = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_roomtypes_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_themes_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThemeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_themes_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_transfertypes_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferTypeId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MaxPassengers = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_transfertypes_cache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunhotels_rooms_cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HotelCacheId = table.Column<Guid>(type: "uuid", nullable: false),
                    HotelId = table.Column<int>(type: "integer", nullable: false),
                    RoomTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MaxOccupancy = table.Column<int>(type: "integer", nullable: false),
                    MinOccupancy = table.Column<int>(type: "integer", nullable: false),
                    FeatureIds = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    ImageUrls = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunhotels_rooms_cache", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sunhotels_rooms_cache_sunhotels_hotels_cache_HotelCacheId",
                        column: x => x.HotelCacheId,
                        principalTable: "sunhotels_hotels_cache",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId",
                table: "sunhotels_destinations_cache",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId_Language",
                table: "sunhotels_destinations_cache",
                columns: new[] { "DestinationId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_features_cache_FeatureId",
                table: "sunhotels_features_cache",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_features_cache_FeatureId_Language",
                table: "sunhotels_features_cache",
                columns: new[] { "FeatureId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_hotels_cache_Category",
                table: "sunhotels_hotels_cache",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_hotels_cache_City",
                table: "sunhotels_hotels_cache",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_hotels_cache_CountryCode",
                table: "sunhotels_hotels_cache",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_hotels_cache_HotelId",
                table: "sunhotels_hotels_cache",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_hotels_cache_HotelId_Language",
                table: "sunhotels_hotels_cache",
                columns: new[] { "HotelId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_hotels_cache_ResortId",
                table: "sunhotels_hotels_cache",
                column: "ResortId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_languages_cache_LanguageCode",
                table: "sunhotels_languages_cache",
                column: "LanguageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_meals_cache_MealId",
                table: "sunhotels_meals_cache",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_meals_cache_MealId_Language",
                table: "sunhotels_meals_cache",
                columns: new[] { "MealId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_notetypes_cache_NoteTypeId_NoteCategory",
                table: "sunhotels_notetypes_cache",
                columns: new[] { "NoteTypeId", "NoteCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_notetypes_cache_NoteTypeId_NoteCategory_Language",
                table: "sunhotels_notetypes_cache",
                columns: new[] { "NoteTypeId", "NoteCategory", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_resorts_cache_ResortId",
                table: "sunhotels_resorts_cache",
                column: "ResortId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_resorts_cache_ResortId_Language",
                table: "sunhotels_resorts_cache",
                columns: new[] { "ResortId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_rooms_cache_HotelCacheId",
                table: "sunhotels_rooms_cache",
                column: "HotelCacheId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_rooms_cache_HotelId",
                table: "sunhotels_rooms_cache",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_rooms_cache_RoomTypeId",
                table: "sunhotels_rooms_cache",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_roomtypes_cache_RoomTypeId",
                table: "sunhotels_roomtypes_cache",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_roomtypes_cache_RoomTypeId_Language",
                table: "sunhotels_roomtypes_cache",
                columns: new[] { "RoomTypeId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_themes_cache_ThemeId",
                table: "sunhotels_themes_cache",
                column: "ThemeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_transfertypes_cache_TransferTypeId",
                table: "sunhotels_transfertypes_cache",
                column: "TransferTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_transfertypes_cache_TransferTypeId_Language",
                table: "sunhotels_transfertypes_cache",
                columns: new[] { "TransferTypeId", "Language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sunhotels_destinations_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_features_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_languages_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_meals_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_notetypes_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_resorts_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_rooms_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_roomtypes_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_themes_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_transfertypes_cache");

            migrationBuilder.DropTable(
                name: "sunhotels_hotels_cache");
        }
    }
}

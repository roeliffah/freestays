using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSunHotelsCacheEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId",
                table: "sunhotels_destinations_cache");

            migrationBuilder.DropIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId_Language",
                table: "sunhotels_destinations_cache");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "sunhotels_transfertypes_cache");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "sunhotels_transfertypes_cache");

            migrationBuilder.DropColumn(
                name: "MaxPassengers",
                table: "sunhotels_transfertypes_cache");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "sunhotels_roomtypes_cache");

            migrationBuilder.DropColumn(
                name: "MaxOccupancy",
                table: "sunhotels_roomtypes_cache");

            migrationBuilder.DropColumn(
                name: "MinOccupancy",
                table: "sunhotels_roomtypes_cache");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "sunhotels_notetypes_cache");

            migrationBuilder.DropColumn(
                name: "EnglishLabel",
                table: "sunhotels_meals_cache");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "sunhotels_meals_cache");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "sunhotels_meals_cache");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "sunhotels_features_cache");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "sunhotels_features_cache");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "sunhotels_features_cache");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "sunhotels_destinations_cache");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "sunhotels_destinations_cache",
                newName: "TimeZone");

            migrationBuilder.AddColumn<string>(
                name: "CountryName",
                table: "sunhotels_resorts_cache",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Labels",
                table: "sunhotels_meals_cache",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryId",
                table: "sunhotels_destinations_cache",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinationCode",
                table: "sunhotels_destinations_cache",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId",
                table: "sunhotels_destinations_cache",
                column: "DestinationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId",
                table: "sunhotels_destinations_cache");

            migrationBuilder.DropColumn(
                name: "CountryName",
                table: "sunhotels_resorts_cache");

            migrationBuilder.DropColumn(
                name: "Labels",
                table: "sunhotels_meals_cache");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "sunhotels_destinations_cache");

            migrationBuilder.DropColumn(
                name: "DestinationCode",
                table: "sunhotels_destinations_cache");

            migrationBuilder.RenameColumn(
                name: "TimeZone",
                table: "sunhotels_destinations_cache",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "sunhotels_transfertypes_cache",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "sunhotels_transfertypes_cache",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxPassengers",
                table: "sunhotels_transfertypes_cache",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "sunhotels_roomtypes_cache",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxOccupancy",
                table: "sunhotels_roomtypes_cache",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinOccupancy",
                table: "sunhotels_roomtypes_cache",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "sunhotels_notetypes_cache",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnglishLabel",
                table: "sunhotels_meals_cache",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "sunhotels_meals_cache",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "sunhotels_meals_cache",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "sunhotels_features_cache",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "sunhotels_features_cache",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "sunhotels_features_cache",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "sunhotels_destinations_cache",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId",
                table: "sunhotels_destinations_cache",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_sunhotels_destinations_cache_DestinationId_Language",
                table: "sunhotels_destinations_cache",
                columns: new[] { "DestinationId", "Language" },
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturedContentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeaturedDestinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DestinationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 999),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Season = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Image = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturedDestinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeaturedHotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HotelId = table.Column<Guid>(type: "uuid", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 999),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Season = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CampaignName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturedHotels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturedHotels_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedDestinations_DestinationId",
                table: "FeaturedDestinations",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedDestinations_Priority",
                table: "FeaturedDestinations",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedDestinations_Season",
                table: "FeaturedDestinations",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedDestinations_Status",
                table: "FeaturedDestinations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedHotels_HotelId",
                table: "FeaturedHotels",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedHotels_Priority",
                table: "FeaturedHotels",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedHotels_Season",
                table: "FeaturedHotels",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedHotels_Status",
                table: "FeaturedHotels",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedHotels_ValidFrom_ValidUntil",
                table: "FeaturedHotels",
                columns: new[] { "ValidFrom", "ValidUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeaturedDestinations");

            migrationBuilder.DropTable(
                name: "FeaturedHotels");
        }
    }
}

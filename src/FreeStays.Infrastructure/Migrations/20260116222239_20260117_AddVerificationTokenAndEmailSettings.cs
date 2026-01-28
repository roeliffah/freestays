using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260117_AddVerificationTokenAndEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL requires USING for type casts when altering column types
            migrationBuilder.Sql(
                "ALTER TABLE hotel_bookings ALTER COLUMN room_type_id TYPE integer USING NULLIF(room_type_id, '')::integer;");
            migrationBuilder.Sql(
                "UPDATE hotel_bookings SET room_type_id = 0 WHERE room_type_id IS NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE hotel_bookings ALTER COLUMN room_type_id SET NOT NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE hotel_bookings ALTER COLUMN room_type_id SET DEFAULT 0;");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationCode",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MealId",
                table: "hotel_bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreBookCode",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "hotel_bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SmtpHost = table.Column<string>(type: "text", nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpUsername = table.Column<string>(type: "text", nullable: false),
                    SmtpPassword = table.Column<string>(type: "text", nullable: false),
                    FromEmail = table.Column<string>(type: "text", nullable: false),
                    FromName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "ConfirmationCode",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "MealId",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "PreBookCode",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "bookings");

            // Revert column type back to string (varchar(100))
            migrationBuilder.Sql(
                "ALTER TABLE hotel_bookings ALTER COLUMN room_type_id DROP DEFAULT;");
            migrationBuilder.Sql(
                "ALTER TABLE hotel_bookings ALTER COLUMN room_type_id DROP NOT NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE hotel_bookings ALTER COLUMN room_type_id TYPE character varying(100) USING room_type_id::text;");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelBookingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreBookCode",
                table: "hotel_bookings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConfirmationCode",
                table: "hotel_bookings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicies",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmationEmailSent",
                table: "hotel_bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationEmailSentAt",
                table: "hotel_bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelAddress",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelNotes",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelPhone",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceRef",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealName",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SunHotelsBookingDate",
                table: "hotel_bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Voucher",
                table: "hotel_bookings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationPolicies",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmationEmailSent",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmationEmailSentAt",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "HotelAddress",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "HotelNotes",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "HotelPhone",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "InvoiceRef",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "MealName",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "SunHotelsBookingDate",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "Voucher",
                table: "hotel_bookings");

            migrationBuilder.AlterColumn<string>(
                name: "PreBookCode",
                table: "hotel_bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConfirmationCode",
                table: "hotel_bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}

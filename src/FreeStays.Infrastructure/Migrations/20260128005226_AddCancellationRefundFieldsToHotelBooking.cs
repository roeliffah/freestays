using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationRefundFieldsToHotelBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CancellationPercentage",
                table: "hotel_bookings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicyText",
                table: "hotel_bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreeCancellationDeadline",
                table: "hotel_bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRefundable",
                table: "hotel_bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxRefundableAmount",
                table: "hotel_bookings",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationPercentage",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "CancellationPolicyText",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "FreeCancellationDeadline",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "IsRefundable",
                table: "hotel_bookings");

            migrationBuilder.DropColumn(
                name: "MaxRefundableAmount",
                table: "hotel_bookings");
        }
    }
}

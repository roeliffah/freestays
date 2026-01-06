using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponKindAndPricingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assigned_email",
                table: "coupons",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_user_id",
                table: "coupons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "kind",
                table: "coupons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "price_amount",
                table: "coupons",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                table: "coupons",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "stripe_payment_intent_id",
                table: "coupons",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "used_at",
                table: "coupons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "used_by_email",
                table: "coupons",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "used_by_user_id",
                table: "coupons",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "assigned_email",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "assigned_user_id",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "price_amount",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "price_currency",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "stripe_payment_intent_id",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "used_by_email",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "used_by_user_id",
                table: "coupons");
        }
    }
}

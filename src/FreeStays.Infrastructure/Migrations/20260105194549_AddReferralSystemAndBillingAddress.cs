using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralSystemAndBillingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_address",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_city",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_country",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_phone",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_postal_code",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "referral_code",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "referred_by_user_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "referral_earnings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    referrer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referred_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    status = table.Column<int>(type: "integer", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_earnings", x => x.id);
                    table.ForeignKey(
                        name: "FK_referral_earnings_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_referral_earnings_users_referred_user_id",
                        column: x => x.referred_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referral_earnings_users_referrer_user_id",
                        column: x => x.referrer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_referral_code",
                table: "users",
                column: "referral_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_referred_by_user_id",
                table: "users",
                column: "referred_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_booking_id",
                table: "referral_earnings",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_referred_user_id",
                table: "referral_earnings",
                column: "referred_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_referrer_user_id",
                table: "referral_earnings",
                column: "referrer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_status",
                table: "referral_earnings",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_referred_by_user_id",
                table: "users",
                column: "referred_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_referred_by_user_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "referral_earnings");

            migrationBuilder.DropIndex(
                name: "IX_users_referral_code",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_referred_by_user_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "billing_address",
                table: "users");

            migrationBuilder.DropColumn(
                name: "billing_city",
                table: "users");

            migrationBuilder.DropColumn(
                name: "billing_country",
                table: "users");

            migrationBuilder.DropColumn(
                name: "billing_phone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "billing_postal_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "referral_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "referred_by_user_id",
                table: "users");
        }
    }
}

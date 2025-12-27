using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentSettingsStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecretKey",
                table: "PaymentSettings",
                newName: "TestModeSecretKey");

            migrationBuilder.RenameColumn(
                name: "PublicKey",
                table: "PaymentSettings",
                newName: "TestModePublicKey");

            migrationBuilder.AddColumn<string>(
                name: "LiveModePublicKey",
                table: "PaymentSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiveModeSecretKey",
                table: "PaymentSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiveModePublicKey",
                table: "PaymentSettings");

            migrationBuilder.DropColumn(
                name: "LiveModeSecretKey",
                table: "PaymentSettings");

            migrationBuilder.RenameColumn(
                name: "TestModeSecretKey",
                table: "PaymentSettings",
                newName: "SecretKey");

            migrationBuilder.RenameColumn(
                name: "TestModePublicKey",
                table: "PaymentSettings",
                newName: "PublicKey");
        }
    }
}

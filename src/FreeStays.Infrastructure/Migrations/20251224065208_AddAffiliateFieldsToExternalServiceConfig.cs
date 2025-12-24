using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliateFieldsToExternalServiceConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliateCode",
                table: "external_service_configs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntegrationMode",
                table: "external_service_configs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffiliateCode",
                table: "external_service_configs");

            migrationBuilder.DropColumn(
                name: "IntegrationMode",
                table: "external_service_configs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeoSchemaOrgFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessAddress",
                table: "SeoSettings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "SeoSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "SeoSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableFaqSchema",
                table: "SeoSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSearchActionSchema",
                table: "SeoSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HotelAddress",
                table: "SeoSettings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelAggregateRating",
                table: "SeoSettings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelImage",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelName",
                table: "SeoSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelPriceRange",
                table: "SeoSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelSchemaType",
                table: "SeoSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HotelStarRating",
                table: "SeoSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelTelephone",
                table: "SeoSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgLocale",
                table: "SeoSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgSiteName",
                table: "SeoSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgType",
                table: "SeoSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgUrl",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationDescription",
                table: "SeoSettings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationLogo",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "SeoSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationSocialProfiles",
                table: "SeoSettings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationUrl",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchActionTarget",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructuredDataJson",
                table: "SeoSettings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterCard",
                table: "SeoSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterCreator",
                table: "SeoSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterImage",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterSite",
                table: "SeoSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteName",
                table: "SeoSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteSearchActionTarget",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "SeoSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessAddress",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "EnableFaqSchema",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "EnableSearchActionSchema",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelAddress",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelAggregateRating",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelImage",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelName",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelPriceRange",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelSchemaType",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelStarRating",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "HotelTelephone",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OgLocale",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OgSiteName",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OgType",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OgUrl",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationDescription",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationLogo",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationSocialProfiles",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationUrl",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "SearchActionTarget",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "StructuredDataJson",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "TwitterCard",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "TwitterCreator",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "TwitterImage",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "TwitterSite",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "WebsiteName",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "WebsiteSearchActionTarget",
                table: "SeoSettings");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "SeoSettings");
        }
    }
}

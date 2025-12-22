using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreeStays.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFaqSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaqTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FaqId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaqTranslations_Faqs_FaqId",
                        column: x => x.FaqId,
                        principalTable: "Faqs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_Category",
                table: "Faqs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Faqs_Order",
                table: "Faqs",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_FaqTranslations_FaqId_Locale",
                table: "FaqTranslations",
                columns: new[] { "FaqId", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqTranslations");

            migrationBuilder.DropTable(
                name: "Faqs");
        }
    }
}

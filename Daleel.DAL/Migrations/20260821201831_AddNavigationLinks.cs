using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Daleel.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NavigationLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Location = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<int>(type: "int", nullable: true),
                    LabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OpenInNewTab = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinks_IsActive",
                table: "NavigationLinks",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLinks_Location_Section_SortOrder",
                table: "NavigationLinks",
                columns: new[] { "Location", "Section", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NavigationLinks");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagsText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_IsVisible_UpdatedAt",
                table: "SearchDocuments",
                columns: new[] { "IsVisible", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_ItemType_ItemId",
                table: "SearchDocuments",
                columns: new[] { "ItemType", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchDocuments");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPostOutfitColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "OutfitDate",
                table: "Posts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutfitLocation",
                table: "Posts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutfitOccasion",
                table: "Posts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutfitStyle",
                table: "Posts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutfitDate",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OutfitLocation",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OutfitOccasion",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OutfitStyle",
                table: "Posts");
        }
    }
}

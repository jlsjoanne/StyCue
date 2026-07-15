using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTagEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TagCategory",
                table: "Tags",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagCategory",
                table: "Tags");
        }
    }
}

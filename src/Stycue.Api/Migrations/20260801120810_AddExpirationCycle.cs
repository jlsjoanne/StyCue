using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExpirationCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpirationCycle",
                table: "Commissions",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationCycle",
                table: "Commissions");
        }
    }
}

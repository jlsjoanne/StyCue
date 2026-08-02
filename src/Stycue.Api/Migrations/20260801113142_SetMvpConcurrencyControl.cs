using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class SetMvpConcurrencyControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Commissions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "UX_PointTransactions_CommissionSettlement",
                table: "PointTransactions",
                columns: new[] { "ReferenceType", "ReferenceId" },
                unique: true,
                filter: "[ReferenceType] = 1 AND [TransactionType] IN (5, 6, 7)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PointTransactions_CommissionSettlement",
                table: "PointTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Commissions");
        }
    }
}

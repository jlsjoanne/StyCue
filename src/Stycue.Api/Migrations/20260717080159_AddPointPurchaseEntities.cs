using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPointPurchaseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PriceTwd = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    BasePoints = table.Column<int>(type: "int", nullable: false),
                    BonusPoints = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointProducts", x => x.Id);
                    table.CheckConstraint("CK_PointProducts_Points_Valid", "[BasePoints] >= 0 AND [BonusPoints] >= 0 AND [Points] > 0 AND [Points] = [BasePoints] + [BonusPoints]");
                    table.CheckConstraint("CK_PointProducts_PriceTwd_Positive", "[PriceTwd] > 0");
                });

            migrationBuilder.CreateTable(
                name: "PointPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MerchantTradeNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PointProductId = table.Column<int>(type: "int", nullable: false),
                    AmountTwd = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PaymentProvider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Ecpay"),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "CreditCard"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ProviderTradeNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointPurchaseOrders", x => x.Id);
                    table.CheckConstraint("CK_PointPurchaseOrders_AmountTwd_Positive", "[AmountTwd] > 0");
                    table.CheckConstraint("CK_PointPurchaseOrders_Points_Positive", "[Points] > 0");
                    table.ForeignKey(
                        name: "FK_PointPurchaseOrders_PointProducts_PointProductId",
                        column: x => x.PointProductId,
                        principalTable: "PointProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointPurchaseOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PointProducts",
                columns: new[] { "Id", "BasePoints", "BonusPoints", "Code", "DisplayOrder", "IsActive", "Name", "Points", "PriceTwd" },
                values: new object[,]
                {
                    { 1, 100, 0, "POINT_100", 1, true, "基礎點數方案", 100, 49 },
                    { 2, 200, 50, "POINT_250", 2, true, "超值點數方案", 250, 99 },
                    { 3, 400, 100, "POINT_500", 3, true, "熱門點數方案", 500, 199 },
                    { 4, 600, 150, "POINT_750", 4, true, "大容量點數方案", 750, 299 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointProducts_Code",
                table: "PointProducts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointPurchaseOrders_MerchantTradeNo",
                table: "PointPurchaseOrders",
                column: "MerchantTradeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointPurchaseOrders_PointProductId",
                table: "PointPurchaseOrders",
                column: "PointProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PointPurchaseOrders_UserId",
                table: "PointPurchaseOrders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointPurchaseOrders");

            migrationBuilder.DropTable(
                name: "PointProducts");
        }
    }
}

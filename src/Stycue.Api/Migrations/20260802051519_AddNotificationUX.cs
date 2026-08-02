using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stycue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationUX : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Notifications_RecipientUserId_DeduplicationKey",
                table: "Notifications",
                newName: "UX_Notifications_RecipientUserId_DeduplicationKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "UX_Notifications_RecipientUserId_DeduplicationKey",
                table: "Notifications",
                newName: "IX_Notifications_RecipientUserId_DeduplicationKey");
        }
    }
}

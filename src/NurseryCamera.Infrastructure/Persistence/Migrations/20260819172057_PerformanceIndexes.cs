using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurseryCamera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_CameraRooms_RoomId",
                table: "CameraRooms");

            migrationBuilder.CreateIndex(
                name: "IX_ViewingSessions_Status_ExpiresAtUtc",
                table: "ViewingSessions",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RefreshToken",
                table: "Users",
                column: "RefreshToken",
                unique: true,
                filter: "[RefreshToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StreamTokens_Status_ExpiresAtUtc",
                table: "StreamTokens",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredAtUtc",
                table: "OutboxMessages",
                column: "OccurredAtUtc",
                filter: "[ProcessedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CameraRooms_RoomId_ValidToUtc",
                table: "CameraRooms",
                columns: new[] { "RoomId", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CameraHealthChecks_CheckedAtUtc",
                table: "CameraHealthChecks",
                column: "CheckedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ViewingSessions_Status_ExpiresAtUtc",
                table: "ViewingSessions");

            migrationBuilder.DropIndex(
                name: "IX_Users_RefreshToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_StreamTokens_Status_ExpiresAtUtc",
                table: "StreamTokens");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_CameraRooms_RoomId_ValidToUtc",
                table: "CameraRooms");

            migrationBuilder.DropIndex(
                name: "IX_CameraHealthChecks_CheckedAtUtc",
                table: "CameraHealthChecks");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CameraRooms_RoomId",
                table: "CameraRooms",
                column: "RoomId");
        }
    }
}

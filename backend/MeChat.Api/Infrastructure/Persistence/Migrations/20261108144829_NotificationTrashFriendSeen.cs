using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeChat.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationTrashFriendSeen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Notifications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SeenAt",
                table: "FriendRequests",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            // Apply only under approved maintenance write quiescence. EF executes this
            // once with the migration, inside its transaction; never run it at startup.
            migrationBuilder.Sql("""
                EXEC(N'
                    DECLARE @seenAt datetime2(7) = SYSUTCDATETIME();
                    UPDATE [FriendRequests]
                    SET [SeenAt] = @seenAt
                    WHERE [Status] = N''Pending'' AND [SeenAt] IS NULL;
                ');
                """);
        }

        /// <inheritdoc />
        // Destructive to Trash/seen state; review only, never a hot rollback after feature traffic.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SeenAt",
                table: "FriendRequests");
        }
    }
}

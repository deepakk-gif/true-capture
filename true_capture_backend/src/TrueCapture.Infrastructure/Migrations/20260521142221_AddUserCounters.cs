using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCapture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowersCount",
                schema: "identity",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FollowingCount",
                schema: "identity",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PostsCount",
                schema: "identity",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill the new denormalized counters from existing rows so accounts created
            // before this migration report correct totals. FollowStatus.Accepted = 1.
            migrationBuilder.Sql("""
                UPDATE identity."User" u SET
                    "FollowersCount" = (SELECT COUNT(*) FROM social."Follow" f
                                        WHERE f."FolloweeId" = u."Id" AND f."Status" = 1),
                    "FollowingCount" = (SELECT COUNT(*) FROM social."Follow" f
                                        WHERE f."FollowerId" = u."Id" AND f."Status" = 1),
                    "PostsCount"     = (SELECT COUNT(*) FROM social."Post" p
                                        WHERE p."AuthorId" = u."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowersCount",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "FollowingCount",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PostsCount",
                schema: "identity",
                table: "User");
        }
    }
}

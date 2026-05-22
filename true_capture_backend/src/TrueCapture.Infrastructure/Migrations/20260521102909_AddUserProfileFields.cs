using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueCapture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                schema: "identity",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                schema: "identity",
                table: "User",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountType",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Gender",
                schema: "identity",
                table: "User");
        }
    }
}

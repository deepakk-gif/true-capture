using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrueCapture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDevice",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId          = table.Column<long>(type: "bigint", nullable: false),
                    FcmToken        = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DeviceType      = table.Column<string>(type: "character varying(16)",  maxLength: 16,  nullable: true),
                    LastUsedAtUtc   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted       = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion      = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevice_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDevice_FcmToken",
                schema: "identity",
                table: "UserDevice",
                column: "FcmToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevice_UserId",
                schema: "identity",
                table: "UserDevice",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDevice",
                schema: "identity");
        }
    }
}

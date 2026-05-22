using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrueCapture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.CreateTable(
                name: "Conversation",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastMessageAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessagePreview = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    LastMessageSenderId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipant",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    PinnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReadMessageId = table.Column<long>(type: "bigint", nullable: true),
                    UnreadCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "messaging",
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    SenderId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MediaUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    MediaWidth = table.Column<int>(type: "integer", nullable: true),
                    MediaHeight = table.Column<int>(type: "integer", nullable: true),
                    ReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Message_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "messaging",
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Message_Message_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalSchema: "messaging",
                        principalTable: "Message",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Message_User_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageReaction",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageReaction_Message_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "messaging",
                        principalTable: "Message",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageReaction_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_LastMessageAtUtc",
                schema: "messaging",
                table: "Conversation",
                column: "LastMessageAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_ConversationId_UserId",
                schema: "messaging",
                table: "ConversationParticipant",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_UserId",
                schema: "messaging",
                table: "ConversationParticipant",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_ConversationId_Id",
                schema: "messaging",
                table: "Message",
                columns: new[] { "ConversationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Message_ReplyToMessageId",
                schema: "messaging",
                table: "Message",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_SenderId",
                schema: "messaging",
                table: "Message",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReaction_MessageId_UserId",
                schema: "messaging",
                table: "MessageReaction",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageReaction_UserId",
                schema: "messaging",
                table: "MessageReaction",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationParticipant",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "MessageReaction",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "Message",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "Conversation",
                schema: "messaging");
        }
    }
}

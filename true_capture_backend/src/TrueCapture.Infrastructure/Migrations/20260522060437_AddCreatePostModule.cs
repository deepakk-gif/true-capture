using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrueCapture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatePostModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "social",
                table: "Post");

            migrationBuilder.AddColumn<bool>(
                name: "CanPostFakeVsReal",
                schema: "identity",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CreatorScore",
                schema: "identity",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FvrCandidateNotifiedAtUtc",
                schema: "identity",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommentsCount",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                schema: "social",
                table: "Post",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FalseVotesCount",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminPost",
                schema: "social",
                table: "Post",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RemovalReason",
                schema: "social",
                table: "Post",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShareId",
                schema: "social",
                table: "Post",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SharesCount",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrueVotesCount",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                schema: "social",
                table: "Post",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                schema: "social",
                table: "Comment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                schema: "social",
                table: "Comment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ParentCommentId",
                schema: "social",
                table: "Comment",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommentLike",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommentId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_CommentLike", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentLike_Comment_CommentId",
                        column: x => x.CommentId,
                        principalSchema: "social",
                        principalTable: "Comment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentLike_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaAsset",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    CaptureMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_MediaAsset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAsset_User_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostMention",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    MentionedUserId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_PostMention", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMention_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostMention_User_MentionedUserId",
                        column: x => x.MentionedUserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostReference",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PostReference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostReference_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostReport",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    ReporterId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    OtherText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Resolution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResolvedById = table.Column<long>(type: "bigint", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_PostReport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostReport_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostReport_User_ReporterId",
                        column: x => x.ReporterId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostSave",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_PostSave", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSave_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostSave_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostShare",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_PostShare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostShare_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostShare_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostView",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    ViewerId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_PostView", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostView_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostView_User_ViewerId",
                        column: x => x.ViewerId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostVote",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_PostVote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostVote_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostVote_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostMedia",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<long>(type: "bigint", nullable: false),
                    MediaAssetId = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PostMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMedia_MediaAsset_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalSchema: "social",
                        principalTable: "MediaAsset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostMedia_Post_PostId",
                        column: x => x.PostId,
                        principalSchema: "social",
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Post_ShareId",
                schema: "social",
                table: "Post",
                column: "ShareId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Post_Type_Status_Id",
                schema: "social",
                table: "Post",
                columns: new[] { "Type", "Status", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ParentCommentId",
                schema: "social",
                table: "Comment",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentLike_CommentId_UserId",
                schema: "social",
                table: "CommentLike",
                columns: new[] { "CommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentLike_UserId",
                schema: "social",
                table: "CommentLike",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAsset_OwnerId_Status",
                schema: "social",
                table: "MediaAsset",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_MediaAssetId",
                schema: "social",
                table: "PostMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_PostId_Position",
                schema: "social",
                table: "PostMedia",
                columns: new[] { "PostId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMention_MentionedUserId",
                schema: "social",
                table: "PostMention",
                column: "MentionedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostMention_PostId_MentionedUserId",
                schema: "social",
                table: "PostMention",
                columns: new[] { "PostId", "MentionedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostReference_PostId_Position",
                schema: "social",
                table: "PostReference",
                columns: new[] { "PostId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PostReport_PostId",
                schema: "social",
                table: "PostReport",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostReport_ReporterId",
                schema: "social",
                table: "PostReport",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_PostReport_Status_Id",
                schema: "social",
                table: "PostReport",
                columns: new[] { "Status", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSave_PostId",
                schema: "social",
                table: "PostSave",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSave_UserId_PostId",
                schema: "social",
                table: "PostSave",
                columns: new[] { "UserId", "PostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostShare_PostId_Id",
                schema: "social",
                table: "PostShare",
                columns: new[] { "PostId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PostShare_UserId",
                schema: "social",
                table: "PostShare",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostView_PostId_ViewerId",
                schema: "social",
                table: "PostView",
                columns: new[] { "PostId", "ViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostView_ViewerId",
                schema: "social",
                table: "PostView",
                column: "ViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PostVote_PostId_UserId",
                schema: "social",
                table: "PostVote",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostVote_UserId",
                schema: "social",
                table: "PostVote",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Comment_ParentCommentId",
                schema: "social",
                table: "Comment",
                column: "ParentCommentId",
                principalSchema: "social",
                principalTable: "Comment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Comment_ParentCommentId",
                schema: "social",
                table: "Comment");

            migrationBuilder.DropTable(
                name: "CommentLike",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostMedia",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostMention",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostReference",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostReport",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostSave",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostShare",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostView",
                schema: "social");

            migrationBuilder.DropTable(
                name: "PostVote",
                schema: "social");

            migrationBuilder.DropTable(
                name: "MediaAsset",
                schema: "social");

            migrationBuilder.DropIndex(
                name: "IX_Post_ShareId",
                schema: "social",
                table: "Post");

            migrationBuilder.DropIndex(
                name: "IX_Post_Type_Status_Id",
                schema: "social",
                table: "Post");

            migrationBuilder.DropIndex(
                name: "IX_Comment_ParentCommentId",
                schema: "social",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "CanPostFakeVsReal",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatorScore",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "FvrCandidateNotifiedAtUtc",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CommentsCount",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "CoverUrl",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "FalseVotesCount",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "IsAdminPost",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "RemovalReason",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "ShareId",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "SharesCount",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "TrueVotesCount",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                schema: "social",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                schema: "social",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                schema: "social",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                schema: "social",
                table: "Comment");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "social",
                table: "Post",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }
    }
}

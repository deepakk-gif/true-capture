using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Social.Entities;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Modules.Social.Infrastructure;

public sealed class SocialModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder b)
    {
        b.Entity<Follow>(e =>
        {
            e.ToTable("Follow", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            // One edge per (follower, followee).
            e.HasIndex(x => new { x.FollowerId, x.FolloweeId }).IsUnique();
            e.HasIndex(x => x.FolloweeId);
            e.HasOne(x => x.Follower).WithMany()
                .HasForeignKey(x => x.FollowerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Followee).WithMany()
                .HasForeignKey(x => x.FolloweeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MediaAsset>(e =>
        {
            e.ToTable("MediaAsset", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<int>().IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            e.Property(x => x.Url).HasMaxLength(1024).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1024);
            e.Property(x => x.MimeType).HasMaxLength(128).IsRequired();
            e.Property(x => x.CaptureMetadata).HasColumnType("jsonb");
            e.Property(x => x.ErrorCode).HasMaxLength(64);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.OwnerId, x.Status });
            e.HasOne<User>().WithMany()
                .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Post>(e =>
        {
            e.ToTable("Post", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<int>().IsRequired();
            e.Property(x => x.Kind).HasConversion<int>().IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.Caption).HasMaxLength(2200);
            e.Property(x => x.CoverUrl).HasMaxLength(1024);
            e.Property(x => x.RemovalReason).HasMaxLength(500);
            e.Property(x => x.ShareId).HasMaxLength(32).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.ShareId).IsUnique();
            e.HasIndex(x => new { x.AuthorId, x.Id });
            e.HasIndex(x => new { x.Type, x.Status, x.Id });
            e.HasOne(x => x.Author).WithMany()
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostMedia>(e =>
        {
            e.ToTable("PostMedia", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.Position });
            e.HasOne(x => x.Post).WithMany(p => p.Media)
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            // Deleting a post drops PostMedia rows but never the shared MediaAsset.
            e.HasOne(x => x.Media).WithMany()
                .HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PostReference>(e =>
        {
            e.ToTable("PostReference", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(1024).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.Position });
            e.HasOne(x => x.Post).WithMany(p => p.References)
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostMention>(e =>
        {
            e.ToTable("PostMention", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.MentionedUserId }).IsUnique();
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MentionedUser).WithMany()
                .HasForeignKey(x => x.MentionedUserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostLike>(e =>
        {
            e.ToTable("PostLike", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostVote>(e =>
        {
            e.ToTable("PostVote", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostView>(e =>
        {
            e.ToTable("PostView", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.ViewerId }).IsUnique();
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany()
                .HasForeignKey(x => x.ViewerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostSave>(e =>
        {
            e.ToTable("PostSave", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.UserId, x.PostId }).IsUnique();
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostShare>(e =>
        {
            e.ToTable("PostShare", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.Id });
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostReport>(e =>
        {
            e.ToTable("PostReport", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasConversion<int>().IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();
            e.Property(x => x.OtherText).HasMaxLength(1000);
            e.Property(x => x.Resolution).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.Id });
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Reporter).WithMany()
                .HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Comment>(e =>
        {
            e.ToTable("Comment", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.PostId, x.Id });
            e.HasIndex(x => x.ParentCommentId);
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany()
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Parent).WithMany()
                .HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<CommentLike>(e =>
        {
            e.ToTable("CommentLike", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.CommentId, x.UserId }).IsUnique();
            e.HasOne(x => x.Comment).WithMany()
                .HasForeignKey(x => x.CommentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Story>(e =>
        {
            e.ToTable("Story", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.ImageUrl).HasMaxLength(512).IsRequired();
            e.Property(x => x.Caption).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.AuthorId, x.ExpiresAtUtc });
            e.HasOne(x => x.Author).WithMany()
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Notification>(e =>
        {
            e.ToTable("Notification", schema: Schemas.Social);
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<int>().IsRequired();
            e.Property(x => x.Text).HasMaxLength(2000);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.RecipientUserId, x.Id });
            e.HasOne<User>().WithMany()
                .HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Actor).WithMany()
                .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
            // PostId is a loose reference (no FK) — the feed tolerates a deleted post.
        });
    }
}

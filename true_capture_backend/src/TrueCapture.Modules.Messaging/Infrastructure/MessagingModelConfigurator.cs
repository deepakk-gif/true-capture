using Microsoft.EntityFrameworkCore;
using TrueCapture.Infrastructure.Data;
using TrueCapture.Modules.Identity.Entities;
using TrueCapture.Modules.Messaging.Entities;
using TrueCapture.Shared.Constants;

namespace TrueCapture.Modules.Messaging.Infrastructure;

public sealed class MessagingModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder b)
    {
        b.Entity<Conversation>(e =>
        {
            e.ToTable("Conversation", schema: Schemas.Messaging);
            e.HasKey(x => x.Id);
            e.Property(x => x.LastMessagePreview).HasMaxLength(280);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => x.LastMessageAtUtc);
        });

        b.Entity<ConversationParticipant>(e =>
        {
            e.ToTable("ConversationParticipant", schema: Schemas.Messaging);
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            // One membership row per (conversation, user).
            e.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.Conversation).WithMany(c => c.Participants)
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Message>(e =>
        {
            e.ToTable("Message", schema: Schemas.Messaging);
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<int>().IsRequired();
            e.Property(x => x.Text).HasMaxLength(4000);
            e.Property(x => x.MediaUrl).HasMaxLength(1024);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1024);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.ConversationId, x.Id });
            e.HasOne(x => x.Conversation).WithMany()
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sender).WithMany()
                .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReplyTo).WithMany()
                .HasForeignKey(x => x.ReplyToMessageId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MessageReaction>(e =>
        {
            e.ToTable("MessageReaction", schema: Schemas.Messaging);
            e.HasKey(x => x.Id);
            e.Property(x => x.Emoji).HasMaxLength(16).IsRequired();
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();
            e.HasOne(x => x.Message).WithMany(m => m.Reactions)
                .HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

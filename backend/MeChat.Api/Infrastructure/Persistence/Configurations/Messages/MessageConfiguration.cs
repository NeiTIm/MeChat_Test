using MeChat.Api.Features.Messages.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Messages;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> e)
    {
        e.HasIndex(x => new { x.ConversationId, x.Sequence }).IsUnique();
        e.HasIndex(x => new { x.SenderUserId, x.ClientMessageId }).IsUnique();
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId);
    }
}

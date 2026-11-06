using MeChat.Api.Features.Conversations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Conversations;

public sealed class MessageRequestConfiguration : IEntityTypeConfiguration<MessageRequest>
{
    public void Configure(EntityTypeBuilder<MessageRequest> e)
    {
        e.HasIndex(x => new { x.ReceiverUserId, x.Status, x.CreatedAt });
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
    }
}

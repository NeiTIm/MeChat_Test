using MeChat.Api.Features.Conversations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Conversations;

public sealed class PrivateChatAccessConfiguration : IEntityTypeConfiguration<PrivateChatAccess>
{
    public void Configure(EntityTypeBuilder<PrivateChatAccess> e)
    {
        e.HasKey(x => new { x.ConversationId, x.SenderUserId, x.ReceiverUserId });
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
    }
}

using MeChat.Api.Features.Conversations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Conversations;

public sealed class PrivateConversationPairConfiguration : IEntityTypeConfiguration<PrivateConversationPair>
{
    public void Configure(EntityTypeBuilder<PrivateConversationPair> e)
    {
        e.HasKey(x => x.ConversationId);
        e.HasIndex(x => new { x.UserId1, x.UserId2 }).IsUnique();
        e.HasOne<Conversation>().WithOne().HasForeignKey<PrivateConversationPair>(x => x.ConversationId);
    }
}

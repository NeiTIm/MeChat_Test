using MeChat.Api.Features.Conversations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Conversations;

public sealed class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> e)
    {
        e.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique().HasFilter("[LeftAt] IS NULL");
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
    }
}

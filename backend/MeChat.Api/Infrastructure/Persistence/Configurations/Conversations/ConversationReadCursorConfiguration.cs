using MeChat.Api.Features.Conversations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Conversations;

public sealed class ConversationReadCursorConfiguration : IEntityTypeConfiguration<ConversationReadCursor>
{
    public void Configure(EntityTypeBuilder<ConversationReadCursor> e)
    {
        e.HasKey(x => new { x.ConversationId, x.UserId });
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
    }
}

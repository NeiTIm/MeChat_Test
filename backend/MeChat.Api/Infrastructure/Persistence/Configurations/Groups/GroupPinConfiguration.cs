using MeChat.Api.Features.Groups.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Groups;

public sealed class GroupPinConfiguration : IEntityTypeConfiguration<GroupPin>
{
    public void Configure(EntityTypeBuilder<GroupPin> e)
    {
        e.HasIndex(x => new { x.ConversationId, x.MessageId }).IsUnique();
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
        e.HasOne<Message>().WithMany().HasForeignKey(x => x.MessageId);
    }
}

using MeChat.Api.Features.Messages.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Messages;

public sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> e)
    {
        e.HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();
        e.HasOne<Message>().WithMany().HasForeignKey(x => x.MessageId);
    }
}

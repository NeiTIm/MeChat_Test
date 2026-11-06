using MeChat.Api.Features.Messages.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Messages;

public sealed class MessageEditHistoryConfiguration : IEntityTypeConfiguration<MessageEditHistory>
{
    public void Configure(EntityTypeBuilder<MessageEditHistory> e)
    {
        e.HasIndex(x => new { x.MessageId, x.Version }).IsUnique();
        e.HasOne<Message>().WithMany().HasForeignKey(x => x.MessageId);
    }
}

using MeChat.Api.Features.Messages.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Messages;

public sealed class MessageUserStateConfiguration : IEntityTypeConfiguration<MessageUserState>
{
    public void Configure(EntityTypeBuilder<MessageUserState> e)
    {
        e.HasKey(x => new { x.MessageId, x.UserId });
        e.HasOne<Message>().WithMany().HasForeignKey(x => x.MessageId);
    }
}

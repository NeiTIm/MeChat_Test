using MeChat.Api.Features.Messages.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Messages;

public sealed class MessageDeliveryConfiguration : IEntityTypeConfiguration<MessageDelivery>
{
    public void Configure(EntityTypeBuilder<MessageDelivery> e)
    {
        e.HasKey(x => new { x.MessageId, x.RecipientUserId });
        e.HasOne<Message>().WithMany().HasForeignKey(x => x.MessageId);
    }
}

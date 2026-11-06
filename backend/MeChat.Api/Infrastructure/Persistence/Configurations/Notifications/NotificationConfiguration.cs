using MeChat.Api.Features.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> e)
    {
        e.HasIndex(x => new { x.RecipientUserId, x.CreatedAt });
        e.HasOne<User>().WithMany().HasForeignKey(x => x.RecipientUserId);
    }
}

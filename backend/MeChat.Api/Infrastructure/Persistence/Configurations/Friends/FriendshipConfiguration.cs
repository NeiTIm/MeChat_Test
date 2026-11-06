using MeChat.Api.Features.Friends.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Friends;

public sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> e)
    {
        e.HasIndex(x => new { x.UserId1, x.UserId2 }).IsUnique().HasFilter("[RemovedAt] IS NULL");
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId1);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId2);
    }
}

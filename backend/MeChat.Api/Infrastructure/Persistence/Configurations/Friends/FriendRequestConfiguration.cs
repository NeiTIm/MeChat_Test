using MeChat.Api.Features.Friends.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Friends;

public sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> e)
    {
        e.Property(x => x.SeenAt).HasPrecision(7);
        e.HasIndex(x => new { x.PairUserId1, x.PairUserId2 }).IsUnique().HasFilter("[Status] = 'Pending'");
        e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.ReceiverUserId);
    }
}

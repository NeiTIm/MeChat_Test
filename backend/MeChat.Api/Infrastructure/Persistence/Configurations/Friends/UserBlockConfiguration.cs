using MeChat.Api.Features.Friends.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Friends;

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> e)
    {
        e.HasIndex(x => new { x.BlockerUserId, x.BlockedUserId }).IsUnique().HasFilter("[UnblockedAt] IS NULL");
        e.HasOne<User>().WithMany().HasForeignKey(x => x.BlockerUserId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.BlockedUserId);
    }
}

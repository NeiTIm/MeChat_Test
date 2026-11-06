using MeChat.Api.Features.Groups.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Groups;

public sealed class GroupInviteLinkConfiguration : IEntityTypeConfiguration<GroupInviteLink>
{
    public void Configure(EntityTypeBuilder<GroupInviteLink> e)
    {
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
        e.HasOne<ConversationMember>().WithMany().HasForeignKey(x => x.CreatedByMembershipId);
    }
}

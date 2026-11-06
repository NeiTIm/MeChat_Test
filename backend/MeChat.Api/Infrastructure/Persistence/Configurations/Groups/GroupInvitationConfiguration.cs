using MeChat.Api.Features.Groups.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Groups;

public sealed class GroupInvitationConfiguration : IEntityTypeConfiguration<GroupInvitation>
{
    public void Configure(EntityTypeBuilder<GroupInvitation> e)
    {
        e.HasIndex(x => new { x.InviteeUserId, x.Status, x.CreatedAt });
        e.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId);
    }
}

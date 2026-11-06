using MeChat.Api.Features.Groups.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Groups;

public sealed class GroupProfileConfiguration : IEntityTypeConfiguration<GroupProfile>
{
    public void Configure(EntityTypeBuilder<GroupProfile> e)
    {
        e.HasKey(x => x.ConversationId);
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasOne<Conversation>().WithOne().HasForeignKey<GroupProfile>(x => x.ConversationId);
    }
}

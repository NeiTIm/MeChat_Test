using MeChat.Api.Features.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Admin;

public sealed class AccountAppealConfiguration : IEntityTypeConfiguration<AccountAppeal>
{
    public void Configure(EntityTypeBuilder<AccountAppeal> e)
    {
        e.HasOne<AccountSuspension>().WithMany().HasForeignKey(x => x.SuspensionId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
    }
}

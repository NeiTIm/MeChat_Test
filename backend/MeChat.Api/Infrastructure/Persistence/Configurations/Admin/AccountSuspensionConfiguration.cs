using MeChat.Api.Features.Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Admin;

public sealed class AccountSuspensionConfiguration : IEntityTypeConfiguration<AccountSuspension>
{
    public void Configure(EntityTypeBuilder<AccountSuspension> e)
    {
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.AdminId);
    }
}

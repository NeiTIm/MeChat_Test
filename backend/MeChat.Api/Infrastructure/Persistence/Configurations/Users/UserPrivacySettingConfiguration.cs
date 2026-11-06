using MeChat.Api.Features.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Users;

public sealed class UserPrivacySettingConfiguration : IEntityTypeConfiguration<UserPrivacySetting>
{
    public void Configure(EntityTypeBuilder<UserPrivacySetting> e)
    {
        e.HasKey(x => x.UserId);
        e.HasOne<User>().WithOne().HasForeignKey<UserPrivacySetting>(x => x.UserId);
    }
}

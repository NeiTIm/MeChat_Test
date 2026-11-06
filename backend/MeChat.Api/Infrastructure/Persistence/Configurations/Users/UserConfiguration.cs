using MeChat.Api.Features.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Users;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.HasIndex(x => x.NormalizedEmail).IsUnique();
        e.HasIndex(x => x.NormalizedUsername).IsUnique();
        e.Property(x => x.RowVersion).IsRowVersion();
        e.Property(x => x.Email).HasMaxLength(320);
        e.Property(x => x.Username).HasMaxLength(50);
        e.Property(x => x.DisplayName).HasMaxLength(100);
    }
}

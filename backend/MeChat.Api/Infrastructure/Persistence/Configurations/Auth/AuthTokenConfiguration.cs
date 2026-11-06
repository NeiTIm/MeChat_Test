using MeChat.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeChat.Api.Infrastructure.Persistence.Configurations.Auth;

public sealed class AuthTokenConfiguration : IEntityTypeConfiguration<AuthToken>
{
    public void Configure(EntityTypeBuilder<AuthToken> e)
    {
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
    }
}

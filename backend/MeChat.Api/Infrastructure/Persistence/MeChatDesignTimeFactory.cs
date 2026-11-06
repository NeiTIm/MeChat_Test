using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MeChat.Api.Infrastructure.Persistence;

// Design-time tooling never opens a connection to discover or infer another target.
public sealed class MeChatDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(DatabaseTarget.Resolve(config)).Options);
    }
}

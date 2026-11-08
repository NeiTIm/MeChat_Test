using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Bcr.SqlTests;

// Optional building block for a future approved HTTP test host. Not production DI.
// Validation must succeed BEFORE registrations change. No host/connection is started here.
public static class BcrTestDbOverride
{
    public static void UseApprovedBcrDatabase(this IServiceCollection services,
        string? approval, string? host, string? database, string? connection)
    {
        var validated = SqlTestTarget.Validate(approval, host, database, connection);
        services.RemoveAll<AppDbContext>();
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(validated));
    }
}

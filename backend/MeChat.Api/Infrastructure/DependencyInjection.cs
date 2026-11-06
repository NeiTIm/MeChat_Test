
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Infrastructure;

/// <summary>
/// Registers shared infrastructure services for MeChat.
/// </summary>
public static class InfrastructureDependencyInjection
{
    // =====================================================
    // REGISTER INFRASTRUCTURE
    // =====================================================

    public static IServiceCollection AddMeChatInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // -------------------------------------------------
        // DATABASE - SQL SERVER
        // -------------------------------------------------

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                DatabaseTarget.Resolve(configuration));
        });

        // -------------------------------------------------
        // TIME PROVIDER
        // -------------------------------------------------

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        return services;
    }
}

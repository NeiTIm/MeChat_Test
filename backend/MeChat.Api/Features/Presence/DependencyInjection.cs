
using MeChat.Api.Features.Presence.Repositories;
using MeChat.Api.Features.Presence.Repositories.Interfaces;

namespace MeChat.Api.Features.Presence;

/// <summary>
/// Registers dependencies required by the Presence feature.
/// </summary>
public static class PresenceDependencyInjection
{
    // =====================================================
    // REGISTER PRESENCE FEATURE
    // =====================================================

    public static IServiceCollection AddPresenceFeature(
        this IServiceCollection services)
    {
        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IPresenceRepository,
            PresenceRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<PresenceService>();

        return services;
    }
}

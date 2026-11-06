
using Microsoft.Extensions.DependencyInjection;

using MeChat.Api.Realtime.Interfaces;
using MeChat.Api.Realtime.Publishers;

namespace MeChat.Api.Realtime;

/// <summary>
/// Registers SignalR and realtime infrastructure.
/// </summary>
public static class RealtimeDependencyInjection
{
    // =====================================================
    // REGISTER REALTIME
    // =====================================================

    public static IServiceCollection AddMeChatRealtime(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // -------------------------------------------------
        // SIGNALR
        // -------------------------------------------------

        services.AddSignalR();

        // -------------------------------------------------
        // REALTIME PUBLISHER
        // -------------------------------------------------

        services.AddScoped<
            IRealtimePublisher,
            SignalRRealtimePublisher>();

        // -------------------------------------------------
        // STATE EVENTS
        // -------------------------------------------------

        services.AddScoped<StateEvents>();

        // -------------------------------------------------
        // ACTIVE SESSION CONNECTIONS
        // -------------------------------------------------

        services.AddSingleton<SessionConnections>();

        return services;
    }
}

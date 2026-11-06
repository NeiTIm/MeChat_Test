
using Microsoft.Extensions.DependencyInjection;

using MeChat.Api.Features.Notifications.Repositories;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;

namespace MeChat.Api.Features.Notifications;

/// <summary>
/// Registers repositories and services required
/// by the Notifications feature.
/// </summary>
public static class NotificationsDependencyInjection
{
    // =====================================================
    // REGISTER NOTIFICATIONS FEATURE
    // =====================================================

    public static IServiceCollection AddNotificationsFeature(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            INotificationRepository,
            NotificationRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<NotificationService>();

        return services;
    }
}

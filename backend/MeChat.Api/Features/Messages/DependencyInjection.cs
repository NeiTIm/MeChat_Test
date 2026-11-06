
using MeChat.Api.Features.Messages.Repositories;
using MeChat.Api.Features.Messages.Repositories.Interfaces;

namespace MeChat.Api.Features.Messages;

/// <summary>
/// Registers repositories and services
/// required by the Messages feature.
/// </summary>
public static class MessagesDependencyInjection
{
    // =====================================================
    // REGISTER MESSAGES FEATURE
    // =====================================================

    public static IServiceCollection AddMessagesFeature(
        this IServiceCollection services)
    {
        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IMessageActionRepository,
            MessageActionRepository>();

        services.AddScoped<
            IMessageStatusRepository,
            MessageStatusRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<MessageActionService>();

        services.AddScoped<MessageStatusService>();

        services.AddScoped<UnreadSummaryService>();

        return services;
    }
}

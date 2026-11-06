
using MeChat.Api.Features.Conversations.Repositories;
using MeChat.Api.Features.Conversations.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence.Transactions;

namespace MeChat.Api.Features.Conversations;

/// <summary>
/// Registers repositories, transaction handling,
/// and services required by the Conversations feature.
/// </summary>
public static class ConversationsDependencyInjection
{
    // =====================================================
    // REGISTER CONVERSATIONS FEATURE
    // =====================================================

    public static IServiceCollection AddConversationsFeature(
        this IServiceCollection services)
    {
        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IPrivateChatRepository,
            PrivateChatRepository>();

        services.AddScoped<
            IMessageRequestRepository,
            MessageRequestRepository>();

        // -------------------------------------------------
        // UNIT OF WORK
        // -------------------------------------------------

        // Preserve the existing scoped Unit of Work.
        // It uses the application's scoped AppDbContext.

        services.AddScoped<
            IUnitOfWork,
            EfUnitOfWork>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<PrivateChatService>();

        services.AddScoped<MessageRequestService>();

        return services;
    }
}

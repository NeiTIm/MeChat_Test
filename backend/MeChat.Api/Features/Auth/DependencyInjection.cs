
using MeChat.Api.Features.Auth.Repositories;
using MeChat.Api.Features.Auth.Repositories.Interfaces;

namespace MeChat.Api.Features.Auth;

/// <summary>
/// Dependency Injection registration for the Auth feature.
/// </summary>
public static class AuthDependencyInjection
{
    // =====================================================
    // REGISTER AUTH FEATURE
    // =====================================================

    public static IServiceCollection AddAuthFeature(
        this IServiceCollection services)
    {
        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IAuthRepository,
            AuthRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<AuthService>();

        // -------------------------------------------------
        // EMAIL
        // -------------------------------------------------

        // Preserve the existing development mail sender.
        // Production email configuration will be handled
        // separately when its provider is introduced.

        services.AddScoped<
            IAuthMailSender,
            DevMailSink>();

        return services;
    }
}

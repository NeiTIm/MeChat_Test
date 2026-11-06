
using MeChat.Api.Features.Users.Repositories;
using MeChat.Api.Features.Users.Repositories.Interfaces;

namespace MeChat.Api.Features.Users;

/// <summary>
/// Registers dependencies required by the Users feature.
/// </summary>
public static class UsersDependencyInjection
{
    // =====================================================
    // REGISTER USERS FEATURE
    // =====================================================

    public static IServiceCollection AddUsersFeature(
        this IServiceCollection services)
    {
        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IUserRepository,
            UserRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<UserService>();

        return services;
    }
}

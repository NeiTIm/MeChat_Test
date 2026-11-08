
using MeChat.Api.Features.Friends.Repositories;
using MeChat.Api.Features.Friends.Repositories.Interfaces;

namespace MeChat.Api.Features.Friends;

/// <summary>
/// Registers the repositories and services
/// required by the Friends feature.
/// </summary>
public static class FriendsDependencyInjection
{
    // =====================================================
    // REGISTER FRIENDS FEATURE
    // =====================================================

    public static IServiceCollection AddFriendsFeature(
        this IServiceCollection services)
    {
        services.AddOptions<MeChat.Api.Configurations.BcrFeatureOptions>()
            .BindConfiguration(MeChat.Api.Configurations.BcrFeatureOptions.SectionName);
        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IFriendRepository,
            FriendRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<FriendService>();

        return services;
    }
}

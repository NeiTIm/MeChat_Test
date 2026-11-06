
using MeChat.Api.Features.Groups.Repositories;
using MeChat.Api.Features.Groups.Repositories.Interfaces;

namespace MeChat.Api.Features.Groups;

/// <summary>
/// Registers repositories and services required by
/// the Groups feature.
/// </summary>
public static class GroupsDependencyInjection
{
    // =====================================================
    // REGISTER GROUPS FEATURE
    // =====================================================

    public static IServiceCollection AddGroupsFeature(
        this IServiceCollection services)
    {
        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IGroupRepository,
            GroupRepository>();

        services.AddScoped<
            IAdvancedGroupRepository,
            AdvancedGroupRepository>();

        services.AddScoped<
            IGroupInviteLinkRepository,
            GroupInviteLinkRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<GroupService>();

        services.AddScoped<AdvancedGroupService>();

        services.AddScoped<GroupInviteLinkService>();

        return services;
    }
}

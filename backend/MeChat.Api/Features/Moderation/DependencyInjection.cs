
using Microsoft.Extensions.DependencyInjection;

using MeChat.Api.Features.Moderation.Repositories;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;

namespace MeChat.Api.Features.Moderation;

/// <summary>
/// Registers all dependencies for the Moderation feature:
/// repositories, services, retention maintenance options
/// and the background maintenance job.
/// </summary>
public static class ModerationDependencyInjection
{
    // =====================================================
    // REGISTER MODERATION FEATURE
    // =====================================================

    public static IServiceCollection AddModerationFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IAdminRepository,
            AdminRepository>();

        services.AddScoped<
            IAppealRepository,
            AppealRepository>();

        services.AddScoped<
            IReportRepository,
            ReportRepository>();

        services.AddScoped<
            IRetentionRepository,
            RetentionRepository>();

        services.AddScoped<
            IRetentionMaintenanceRepository,
            RetentionMaintenanceRepository>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<AdminService>();

        services.AddScoped<AppealService>();

        services.AddScoped<ReportService>();

        services.AddScoped<RetentionService>();

        services.AddScoped<RetentionMaintenance>();

        // -------------------------------------------------
        // RETENTION MAINTENANCE OPTIONS
        // -------------------------------------------------

        services
            .AddOptions<RetentionMaintenanceOptions>()
            .BindConfiguration("RetentionMaintenance")
            .Validate(
                options =>
                    options.BatchSize is >= 1 and <= 500,
                "Retention batch must be bounded.")
            .Validate(
                options =>
                    !options.EnablePurge ||
                    (
                        options.OperatorUserId != Guid.Empty &&
                        !string.IsNullOrWhiteSpace(
                            options.ApprovalReference) &&
                        options.ApprovalReference.Length <= 500
                    ),
                "Retention purge requires separate operator approval.")
            .ValidateOnStart();

        // -------------------------------------------------
        // BACKGROUND JOB
        // -------------------------------------------------

        services.AddHostedService<RetentionMaintenanceJob>();

        return services;
    }
}

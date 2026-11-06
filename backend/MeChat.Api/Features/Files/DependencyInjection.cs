
using Microsoft.Extensions.DependencyInjection;

using MeChat.Api.Features.Files.Repositories;
using MeChat.Api.Features.Files.Repositories.Interfaces;

namespace MeChat.Api.Features.Files;

/// <summary>
/// Registers dependencies for the Files feature:
/// repositories, storage, services, retention options
/// and the background retention job.
/// </summary>
public static class FilesDependencyInjection
{
    // =====================================================
    // REGISTER FILES FEATURE
    // =====================================================

    public static IServiceCollection AddFilesFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // -------------------------------------------------
        // REPOSITORIES
        // -------------------------------------------------

        services.AddScoped<
            IMessageAttachmentRepository,
            MessageAttachmentRepository>();

        services.AddScoped<
            IFileRepository,
            FileRepository>();

        services.AddScoped<
            IFileRetentionRepository,
            FileRetentionRepository>();

        // -------------------------------------------------
        // FILE STORAGE
        // -------------------------------------------------

        // Preserve the existing LocalFileStorage provider.
        // Changing storage providers is outside this refactor.

        services.AddSingleton<
            IFileStorage,
            LocalFileStorage>();

        // -------------------------------------------------
        // SERVICES
        // -------------------------------------------------

        services.AddScoped<FileService>();

        services.AddScoped<MessageAttachmentService>();

        services.AddScoped<FileRetentionService>();

        // -------------------------------------------------
        // FILE RETENTION OPTIONS
        // -------------------------------------------------

        services
            .AddOptions<FileRetentionOptions>()
            .BindConfiguration("FileRetention")
            .Validate(
                options =>
                    options.PendingHours >= 24 &&
                    options.RecalledDays >= 1 &&
                    options.AvatarDays >= 1 &&
                    options.BatchSize is >= 1 and <= 500,
                "File retention must use safe positive grace periods and a bounded batch.")
            .Validate(
                options =>
                    !options.EnableDeletion ||
                    (
                        !string.IsNullOrWhiteSpace(
                            options.ApprovalReference) &&
                        options.ApprovalReference.Length <= 500
                    ),
                "Physical deletion requires a valid approved operation reference.")
            .ValidateOnStart();

        // -------------------------------------------------
        // BACKGROUND JOB
        // -------------------------------------------------

        services.AddHostedService<FileRetentionJob>();

        return services;
    }
}

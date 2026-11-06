
using Microsoft.Extensions.Options;

namespace MeChat.Api.Features.Files;

public sealed class FileRetentionJob(
    IServiceScopeFactory scopes,
    IOptions<FileRetentionOptions> options,
    ILogger<FileRetentionJob> logger)
    : BackgroundService
{
    // =====================================================
    // BACKGROUND FILE RETENTION
    // =====================================================

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // =================================================
        // SCHEDULED FLAG
        // =================================================

        if (!options.Value.RunScheduled)
        {
            return;
        }

        // =================================================
        // HOURLY TIMER
        // =================================================

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(1));

        // Preserve the original behavior:
        // the first iteration runs after one hour.

        while (await timer.WaitForNextTickAsync(
                   stoppingToken))
        {
            try
            {
                // =============================================
                // CREATE DI SCOPE
                // =============================================

                await using var scope =
                    scopes.CreateAsyncScope();

                var service =
                    scope.ServiceProvider
                        .GetRequiredService<FileRetentionService>();

                // =============================================
                // PHYSICAL CLEANUP
                // =============================================

                if (options.Value.EnableDeletion)
                {
                    var removed =
                        await service.SweepAsync(
                            stoppingToken);

                    logger.LogInformation(
                        "File retention sweep completed; removed {Count} files",
                        removed);
                }

                // =============================================
                // DRY RUN
                // =============================================

                else
                {
                    var preview =
                        await service.PreviewAsync(
                            stoppingToken);

                    logger.LogInformation(
                        "File retention dry run: {Count} candidates",
                        preview.Items.Count);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                logger.LogError(
                    error,
                    "File retention job failed; no bypass attempted");
            }
        }
    }
}

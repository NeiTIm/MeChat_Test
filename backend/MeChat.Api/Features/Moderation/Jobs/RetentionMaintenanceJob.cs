
using Microsoft.Extensions.Options;

namespace MeChat.Api.Features.Moderation;

public sealed class RetentionMaintenanceJob(
    IServiceScopeFactory scopes,
    IOptions<RetentionMaintenanceOptions> options,
    ILogger<RetentionMaintenanceJob> logger)
    : BackgroundService
{
    // =====================================================
    // BACKGROUND MAINTENANCE
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
        // 24-HOUR PERIODIC TIMER
        // =================================================

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(24));

        // Preserve existing behavior:
        // the first run occurs after the first 24-hour tick,
        // not immediately on application startup.

        while (await timer.WaitForNextTickAsync(
                   stoppingToken))
        {
            try
            {
                // =============================================
                // CREATE DEPENDENCY INJECTION SCOPE
                // =============================================

                await using var scope =
                    scopes.CreateAsyncScope();

                var provider =
                    scope.ServiceProvider;

                // =============================================
                // APPROVED PURGE MODE
                // =============================================

                if (options.Value.EnablePurge)
                {
                    var maintenance =
                        provider.GetRequiredService<
                            RetentionMaintenance>();

                    var removed =
                        await maintenance.RunAsync(
                            stoppingToken);

                    logger.LogInformation(
                        "Approved retention batch removed {Count} records",
                        removed);
                }

                // =============================================
                // DRY-RUN MODE
                // =============================================

                else
                {
                    var retention =
                        provider.GetRequiredService<
                            RetentionService>();

                    var preview =
                        await retention.PreviewAsync(
                            options.Value.OperatorUserId,
                            stoppingToken);

                    logger.LogInformation(
                        "Retention dry run: authorized={Authorized}, evidence={Evidence}, audit={Audit}",
                        preview is not null,
                        preview?.EvidenceEligible,
                        preview?.AuditEligible);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                // =============================================
                // FAIL CLOSED
                // =============================================

                // Log the exception and leave the remaining
                // database records untouched.
                // Transaction rollback is handled by
                // RetentionMaintenanceRepository.

                logger.LogError(
                    error,
                    "Retention maintenance failed closed");
            }
        }
    }
}

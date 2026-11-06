
using Microsoft.Extensions.Options;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;

namespace MeChat.Api.Features.Moderation;

public sealed class RetentionMaintenance(
    IRetentionMaintenanceRepository repository,
    AdminService admin,
    IOptions<RetentionMaintenanceOptions> configuration,
    TimeProvider clock)
{
    // =====================================================
    // RUN RETENTION MAINTENANCE
    // =====================================================

    public async Task<int> RunAsync(
        CancellationToken ct)
    {
        var options = configuration.Value;

        // =================================================
        // PURGE FEATURE FLAG
        // =================================================

        // Retention purge is disabled by default.
        // Do not access the database or perform deletes
        // when the feature is disabled.

        if (!options.EnablePurge)
        {
            return 0;
        }

        // =================================================
        // APPROVAL VALIDATION
        // =================================================

        if (string.IsNullOrWhiteSpace(
                options.ApprovalReference) ||
            options.OperatorUserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Retention purge requires a separately approved operator and operation reference.");
        }

        // =================================================
        // BATCH VALIDATION
        // =================================================

        if (options.BatchSize <= 0)
        {
            throw new InvalidOperationException(
                "Retention batch size must be positive.");
        }

        // =================================================
        // OPERATION TIMESTAMP
        // =================================================

        var now = clock.GetUtcNow().UtcDateTime;

        // =================================================
        // APPROVED PURGE TRANSACTION
        // =================================================

        // The repository:
        // 1. Validates the target database.
        // 2. Opens a Serializable transaction.
        // 3. Rechecks administrator authorization.
        // 4. Selects eligible records.
        // 5. Rechecks LegalHold and retention rules.
        // 6. Deletes eligible database records.
        // 7. Creates an audit log.
        // 8. Commits the transaction.

        return await repository.PurgeApprovedBatchAsync(
            options.OperatorUserId,
            options.ApprovalReference!,
            options.BatchSize,
            now,
            () => admin.IsAdminAsync(
                options.OperatorUserId,
                ct),
            ct);
    }
}

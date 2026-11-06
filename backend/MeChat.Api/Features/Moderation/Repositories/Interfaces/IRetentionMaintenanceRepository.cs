
namespace MeChat.Api.Features.Moderation.Repositories.Interfaces;

public interface IRetentionMaintenanceRepository
{
    // =====================================================
    // APPROVED RETENTION PURGE
    // =====================================================

    // The authorization callback is evaluated inside
    // the same Serializable transaction as the deletions.
    //
    // The repository must independently verify:
    // - The configured database target
    // - The actual connected database
    // - The operator's authorization
    // - Evidence eligibility
    // - Audit eligibility
    // - Legal hold conditions
    //
    // The method returns the total number of deleted
    // database records, excluding the new audit entry.

    Task<int> PurgeApprovedBatchAsync(
        Guid operatorUserId,
        string approvalReference,
        int batchSize,
        DateTime now,
        Func<Task<bool>> authorizeOperator,
        CancellationToken ct);
}


using System.Data;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;

namespace MeChat.Api.Features.Moderation.Repositories;

public sealed class RetentionMaintenanceRepository(
    AppDbContext db,
    IRetentionRepository retention)
    : IRetentionMaintenanceRepository
{
    // =====================================================
    // APPROVED RETENTION PURGE
    // =====================================================

    public async Task<int> PurgeApprovedBatchAsync(
        Guid operatorUserId,
        string approvalReference,
        int batchSize,
        DateTime now,
        Func<Task<bool>> authorizeOperator,
        CancellationToken ct)
    {
        // =================================================
        // FAIL-CLOSED VALIDATION
        // =================================================

        if (operatorUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(approvalReference))
        {
            throw new InvalidOperationException(
                "Retention purge requires a separately approved operator and operation reference.");
        }

        if (batchSize is < 1 or > 500)
        {
            throw new InvalidOperationException(
                "Retention batch size must be between 1 and 500.");
        }

        ArgumentNullException.ThrowIfNull(authorizeOperator);

        // =================================================
        // DATABASE TARGET VALIDATION
        // =================================================

        DatabaseTarget.Validate(
            db.Database.GetConnectionString());

        await db.Database.OpenConnectionAsync(ct);

        try
        {
            // =================================================
            // VERIFY ACTUAL DATABASE
            // =================================================

            await using (var command =
                db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT DB_NAME()";

                var databaseName =
                    await command.ExecuteScalarAsync(ct)
                        as string;

                if (!string.Equals(
                        databaseName,
                        "MeChat",
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Unexpected retention database.");
                }
            }

            // =================================================
            // SERIALIZABLE TRANSACTION
            // =================================================

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    ct);

            try
            {
                // =============================================
                // AUTHORIZATION INSIDE TRANSACTION
                // =============================================

                if (!await authorizeOperator())
                {
                    throw new InvalidOperationException(
                        "Retention operator is not authorized.");
                }

                var suspended =
                    await db.AccountSuspensions.AnyAsync(
                        suspension =>
                            suspension.UserId == operatorUserId &&
                            suspension.Status == "Active" &&
                            suspension.RevokedAt == null &&
                            (
                                suspension.ExpiresAt == null ||
                                suspension.ExpiresAt > now
                            ),
                        ct);

                if (suspended)
                {
                    throw new InvalidOperationException(
                        "Retention operator is not authorized.");
                }

                // =============================================
                // SELECT ELIGIBLE EVIDENCE IDS
                // =============================================

                var evidenceIds =
                    await retention.GetEligibleEvidenceIdsAsync(
                        now,
                        batchSize,
                        ct);

                // =============================================
                // SELECT ELIGIBLE AUDIT IDS
                // =============================================

                var auditIds =
                    await db.AdminAuditLogs
                        .Where(audit =>
                            !audit.LegalHold &&
                            audit.CreatedAt.AddMonths(12) <= now)
                        .OrderBy(audit => audit.CreatedAt)
                        .ThenBy(audit => audit.Id)
                        .Take(batchSize)
                        .Select(audit => audit.Id)
                        .ToListAsync(ct);

                // =============================================
                // DELETE ELIGIBLE EVIDENCE
                // =============================================

                // Shared retention Repository rechecks:
                // - LegalHold
                // - RetainUntil
                // - Report status
                // - ReviewedAt + 90 days

                var evidenceCount =
                    await retention.DeleteEligibleEvidenceAsync(
                        now,
                        evidenceIds,
                        ct);

                // =============================================
                // DELETE ELIGIBLE AUDIT LOGS
                // =============================================

                var auditCount =
                    await db.AdminAuditLogs
                        .Where(audit =>
                            auditIds.Contains(audit.Id) &&
                            !audit.LegalHold &&
                            audit.CreatedAt.AddMonths(12) <= now)
                        .ExecuteDeleteAsync(ct);

                // =============================================
                // PURGE AUDIT
                // =============================================

                if (evidenceCount + auditCount > 0)
                {
                    db.AdminAuditLogs.Add(
                        new AdminAuditLog
                        {
                            Id = Guid.NewGuid(),
                            AdminUserId = operatorUserId,
                            Action = "RetentionPurge",
                            TargetType = "Retention",
                            CreatedAt = now,
                            Details =
                                $"approval={approvalReference};" +
                                $"evidence={evidenceCount};" +
                                $"audit={auditCount};" +
                                "no physical files removed"
                        });

                    await db.SaveChangesAsync(ct);
                }

                // =============================================
                // COMMIT
                // =============================================

                await transaction.CommitAsync(ct);

                return evidenceCount + auditCount;
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);
                }
                finally
                {
                    db.ChangeTracker.Clear();
                }

                throw;
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}


using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;

namespace MeChat.Api.Features.Moderation.Repositories;

public sealed class RetentionRepository(
    AppDbContext db) : IRetentionRepository
{
    // =====================================================
    // ELIGIBLE EVIDENCE QUERY
    // =====================================================

    private IQueryable<ReportEvidence> EvidenceQuery(
        DateTime now)
    {
        return db.ReportEvidences.Where(evidence =>
            !evidence.LegalHold &&
            evidence.RetainUntil <= now &&

            db.Reports.Any(report =>
                report.Id == evidence.ReportId &&

                (
                    report.Status == "Resolved" ||
                    report.Status == "Dismissed"
                ) &&

                report.ReviewedAt != null &&

                report.ReviewedAt.Value.AddDays(90) <= now &&

                evidence.RetainUntil >=
                    report.ReviewedAt.Value.AddDays(90)));
    }

    // =====================================================
    // RETENTION PREVIEW
    // =====================================================

    public async Task<RetentionPreviewDto> GetPreviewAsync(
        DateTime now,
        CancellationToken ct)
    {
        var evidenceEligible =
            await EvidenceQuery(now).CountAsync(ct);

        var auditEligible =
            await db.AdminAuditLogs.CountAsync(
                audit =>
                    !audit.LegalHold &&
                    audit.CreatedAt.AddMonths(12) <= now,
                ct);

        var evidenceHeld =
            await db.ReportEvidences.CountAsync(
                evidence => evidence.LegalHold,
                ct);

        var auditHeld =
            await db.AdminAuditLogs.CountAsync(
                audit => audit.LegalHold,
                ct);

        return new RetentionPreviewDto(
            evidenceEligible,
            auditEligible,
            evidenceHeld,
            auditHeld);
    }

    // =====================================================
    // RETENTION DRY-RUN PLAN
    // =====================================================

    public Task<List<RetentionPlanItem>> GetPlanItemsAsync(
        string kind,
        DateTime now,
        int offset,
        int take,
        CancellationToken ct)
    {
        if (kind == "Evidence")
        {
            return EvidenceQuery(now)
                .AsNoTracking()
                .OrderBy(evidence => evidence.RetainUntil)
                .ThenBy(evidence => evidence.Id)
                .Skip(offset)
                .Take(take)
                .Select(evidence => new RetentionPlanItem(
                    evidence.Id,
                    evidence.RetainUntil!.Value))
                .ToListAsync(ct);
        }

        if (kind == "Audit")
        {
            return db.AdminAuditLogs
                .AsNoTracking()
                .Where(audit =>
                    !audit.LegalHold &&
                    audit.CreatedAt.AddMonths(12) <= now)
                .OrderBy(audit => audit.CreatedAt)
                .ThenBy(audit => audit.Id)
                .Skip(offset)
                .Take(take)
                .Select(audit => new RetentionPlanItem(
                    audit.Id,
                    audit.CreatedAt.AddMonths(12)))
                .ToListAsync(ct);
        }

        throw new ArgumentException(
            "Invalid retention kind.",
            nameof(kind));
    }

    // =====================================================
    // SERIALIZABLE HOLD TRANSACTION + RETRY
    // =====================================================

    public async Task<bool> ExecuteHoldAtomicAsync(
        Func<Task<bool>> operation,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var transaction =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        ct);

                var success = await operation();

                if (!success)
                {
                    await transaction.RollbackAsync(ct);

                    db.ChangeTracker.Clear();

                    return false;
                }

                await transaction.CommitAsync(ct);

                return true;
            }
            catch (Exception error)
                when (attempt < 2 && IsRetryable(error))
            {
                db.ChangeTracker.Clear();

                await Task.Delay(
                    30 * (attempt + 1),
                    ct);
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }

        throw new InvalidOperationException(
            "Retention hold retry limit reached.");
    }

    // =====================================================
    // LOCK EVIDENCE ROW
    // =====================================================

    public Task<ReportEvidence?> GetEvidenceForUpdateAsync(
        Guid evidenceId,
        CancellationToken ct)
    {
        return db.ReportEvidences
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [ReportEvidences] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {evidenceId}
                """)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // LOCK AUDIT ROW
    // =====================================================

    public Task<AdminAuditLog?> GetAuditForUpdateAsync(
        Guid auditId,
        CancellationToken ct)
    {
        return db.AdminAuditLogs
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [AdminAuditLogs] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {auditId}
                """)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // REPORT OWNERSHIP
    // =====================================================

    public Task<bool> IsReportAssignedToAdminAsync(
        Guid reportId,
        Guid adminId,
        CancellationToken ct)
    {
        return db.Reports.AnyAsync(
            report =>
                report.Id == reportId &&
                report.ReviewedByAdminId == adminId,
            ct);
    }

    // =====================================================
    // ELIGIBLE EVIDENCE IDS FOR PURGE
    // =====================================================

    public Task<List<Guid>> GetEligibleEvidenceIdsAsync(
        DateTime now,
        int batchSize,
        CancellationToken ct)
    {
        return EvidenceQuery(now)
            .OrderBy(evidence => evidence.RetainUntil)
            .ThenBy(evidence => evidence.Id)
            .Take(batchSize)
            .Select(evidence => evidence.Id)
            .ToListAsync(ct);
    }

    // =====================================================
    // DELETE ELIGIBLE EVIDENCE
    // =====================================================

    public Task<int> DeleteEligibleEvidenceAsync(
        DateTime now,
        IReadOnlyCollection<Guid> evidenceIds,
        CancellationToken ct)
    {
        if (evidenceIds.Count == 0)
            return Task.FromResult(0);

        // Re-evaluate all eligibility conditions.
        // IDs alone are never sufficient authorization
        // for deleting retained evidence.

        var ids = evidenceIds.ToArray();

        return EvidenceQuery(now)
            .Where(evidence => ids.Contains(evidence.Id))
            .ExecuteDeleteAsync(ct);
    }

    // =====================================================
    // AUDIT LOG
    // =====================================================

    public void AddAuditLog(AdminAuditLog audit)
    {
        db.AdminAuditLogs.Add(audit);
    }

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // RETRYABLE DATABASE ERRORS
    // =====================================================

    private static bool IsRetryable(Exception error)
    {
        for (Exception? current = error;
             current is not null;
             current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException)
                return true;

            if (current is SqlException sql &&
                sql.Number == 1205)
                return true;
        }

        return false;
    }
}

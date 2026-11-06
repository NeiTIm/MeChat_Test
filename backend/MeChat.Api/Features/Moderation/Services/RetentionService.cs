
using MeChat.Api.Features.Moderation.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation;

public sealed class RetentionService(
    IRetentionRepository repository,
    AdminService admin,
    TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // RETENTION PREVIEW
    // =====================================================

    public async Task<RetentionPreviewDto?> PreviewAsync(
        Guid actor,
        CancellationToken ct)
    {
        // Only authorized administrators may preview
        // evidence and audit retention statistics.

        if (!await admin.IsAdminAsync(actor, ct))
            return null;

        return await repository.GetPreviewAsync(
            Now,
            ct);
    }

    // =====================================================
    // RETENTION DRY-RUN PLAN
    // =====================================================

    public async Task<RetentionPlanDto?> PlanAsync(
        Guid actor,
        string kind,
        int offset,
        int limit,
        CancellationToken ct)
    {
        // =================================================
        // AUTHORIZATION
        // =================================================

        if (!await admin.IsAdminAsync(actor, ct))
            return null;

        // =================================================
        // VALIDATE PAGINATION AND KIND
        // =================================================

        if (offset is < 0 or > 100000)
            return null;

        if (kind is not ("Evidence" or "Audit"))
            return null;

        var pageSize = Math.Clamp(limit, 1, 100);
        var now = Now;

        // =================================================
        // GET ELIGIBLE ITEMS
        // =================================================

        var rows = await repository.GetPlanItemsAsync(
            kind,
            now,
            offset,
            pageSize + 1,
            ct);

        // =================================================
        // WRITE DRY-RUN AUDIT
        // =================================================

        repository.AddAuditLog(
            new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = actor,
                Action = "RetentionDryRun",
                TargetType = kind,
                CreatedAt = now,
                Details =
                    $"offset={offset};limit={pageSize};no deletion"
            });

        await repository.SaveChangesAsync(ct);

        // =================================================
        // PAGINATION
        // =================================================

        var hasMore = rows.Count > pageSize;

        return new RetentionPlanDto(
            kind,
            rows.Take(pageSize).ToList(),
            hasMore ? offset + pageSize : null);
    }

    // =====================================================
    // SET / RELEASE LEGAL HOLD
    // =====================================================

    public Task<bool> SetHoldAsync(
        Guid actor,
        string kind,
        Guid id,
        RetentionHoldRequest input,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE REASON
        // =================================================

        var reason = input.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Length is < 10 or > 1000)
        {
            return Task.FromResult(false);
        }

        // =================================================
        // VALIDATE KIND
        // =================================================

        if (kind is not ("Evidence" or "Audit"))
        {
            return Task.FromResult(false);
        }

        // =================================================
        // TRANSACTION
        // =================================================

        return repository.ExecuteHoldAtomicAsync(
            async () =>
            {
                // Recheck authorization inside the
                // Serializable transaction.

                if (!await admin.IsAdminAsync(actor, ct))
                    return false;

                // =============================================
                // EVIDENCE HOLD
                // =============================================

                if (kind == "Evidence")
                {
                    var evidence =
                        await repository.GetEvidenceForUpdateAsync(
                            id,
                            ct);

                    if (evidence is null)
                        return false;

                    // Only the admin assigned to the report
                    // may modify its evidence legal hold.

                    var assigned =
                        await repository.IsReportAssignedToAdminAsync(
                            evidence.ReportId,
                            actor,
                            ct);

                    if (!assigned)
                        return false;

                    evidence.LegalHold = input.Hold;
                }

                // =============================================
                // AUDIT HOLD
                // =============================================

                else
                {
                    var audit =
                        await repository.GetAuditForUpdateAsync(
                            id,
                            ct);

                    if (audit is null)
                        return false;

                    audit.LegalHold = input.Hold;
                }

                // =============================================
                // AUDIT HOLD CHANGE
                // =============================================

                repository.AddAuditLog(
                    new AdminAuditLog
                    {
                        Id = Guid.NewGuid(),
                        AdminUserId = actor,
                        Action = input.Hold
                            ? "PlaceRetentionHold"
                            : "ReleaseRetentionHold",
                        TargetType = kind,
                        TargetId = id,
                        CreatedAt = Now,
                        Details = reason
                    });

                // =============================================
                // SAVE ATOMICALLY
                // =============================================

                await repository.SaveChangesAsync(ct);

                return true;
            },
            ct);
    }
}

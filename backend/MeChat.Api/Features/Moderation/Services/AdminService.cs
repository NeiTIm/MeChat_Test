
using MeChat.Api.Features.Files;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Realtime;

namespace MeChat.Api.Features.Moderation;

public sealed class AdminService(
    IAdminRepository repository,
    TimeProvider clock,
    IFileStorage storage,
    StateEvents events,
    SessionConnections connections)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // ADMIN AUTHORIZATION
    // =====================================================

    public Task<bool> IsAdminAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.IsAdminAsync(
            actor,
            Now,
            ct);
    }

    // =====================================================
    // ADMIN DASHBOARD
    // =====================================================

    public async Task<AdminDashboardDto?> DashboardAsync(
        Guid actor,
        CancellationToken ct)
    {
        if (!await IsAdminAsync(actor, ct))
            return null;

        var start = Now.Date.AddDays(-6);

        return await repository.GetDashboardAsync(
            start,
            ct);
    }

    // =====================================================
    // ADMIN USERS LIST
    // =====================================================

    public async Task<List<AdminUserDto>?> UsersAsync(
        Guid actor,
        CancellationToken ct,
        int skip = 0)
    {
        if (!await IsAdminAsync(actor, ct))
            return null;

        return await repository.GetUsersAsync(
            Math.Max(0, skip),
            Now,
            ct);
    }

    // =====================================================
    // ADMIN GROUPS LIST
    // =====================================================

    public async Task<List<AdminGroupDto>?> GroupsAsync(
        Guid actor,
        CancellationToken ct,
        int skip = 0)
    {
        if (!await IsAdminAsync(actor, ct))
            return null;

        return await repository.GetGroupsAsync(
            Math.Max(0, skip),
            ct);
    }

    // =====================================================
    // ADMIN REPORTS LIST
    // =====================================================

    public async Task<List<AdminReportDto>?> ReportsAsync(
        Guid actor,
        CancellationToken ct,
        int skip = 0)
    {
        if (!await IsAdminAsync(actor, ct))
            return null;

        return await repository.GetReportsAsync(
            Math.Max(0, skip),
            ct);
    }

    // =====================================================
    // CLAIM REPORT
    // =====================================================

    public Task<bool> ClaimAsync(
        Guid actor,
        Guid reportId,
        CancellationToken ct)
    {
        return repository.ExecuteMutationAsync(
            async () =>
            {
                // =================================================
                // AUTHORIZATION
                // =================================================

                if (!await IsAdminAsync(actor, ct))
                    return false;

                // =================================================
                // LOCK REPORT
                // =================================================

                var report =
                    await repository.GetReportForUpdateAsync(
                        reportId,
                        ct);

                if (report is null ||
                    report.Status != "Pending")
                {
                    return false;
                }

                // Another admin already claimed the report.
                if (report.ReviewedByAdminId is not null &&
                    report.ReviewedByAdminId != actor)
                {
                    return false;
                }

                // =================================================
                // CLAIM IF NOT ALREADY CLAIMED
                // =================================================

                if (report.ReviewedByAdminId is null)
                {
                    report.ReviewedByAdminId = actor;

                    AddAudit(
                        actor,
                        "ClaimReport",
                        "Report",
                        reportId,
                        null);

                    await repository.SaveChangesAsync(ct);
                }

                // Repeated claim by the same admin
                // remains an idempotent success.
                return true;
            },
            ct,
            enableRetry: true);
    }

    // =====================================================
    // GET CLAIMED REPORT CASE
    // =====================================================

    public Task<AdminCaseDto?> CaseAsync(
        Guid actor,
        Guid reportId,
        CancellationToken ct)
    {
        return repository.ExecuteAuditedReadAsync(
            async () =>
            {
                // =================================================
                // ADMIN AUTHORIZATION
                // =================================================

                if (!await IsAdminAsync(actor, ct))
                    return null;

                // =================================================
                // REPORT OWNERSHIP
                // =================================================

                // An admin may only access evidence
                // for reports assigned to that admin.

                var report =
                    await repository.GetClaimedReportAsync(
                        reportId,
                        actor,
                        ct);

                if (report is null)
                    return null;

                // =================================================
                // REPORT EVIDENCE
                // =================================================

                var evidence =
                    await repository.GetReportEvidenceAsync(
                        reportId,
                        ct);

                // =================================================
                // AUDIT SENSITIVE READ
                // =================================================

                AddAudit(
                    actor,
                    "ReadReportEvidence",
                    "Report",
                    reportId,
                    null);

                await repository.SaveChangesAsync(ct);

                return new AdminCaseDto(
                    Map(report),
                    evidence);
            },
            ct);
    }

    // =====================================================
    // READ EVIDENCE FILE
    // =====================================================

    public async Task<FileRead?> EvidenceFileAsync(
        Guid actor,
        Guid reportId,
        Guid evidenceId,
        CancellationToken ct)
    {
        // Hold the opened stream until the audit transaction
        // has committed. Dispose it if the operation fails.
        Stream? openedStream = null;

        try
        {
            var result =
                await repository.ExecuteAuditedReadAsync(
                    async () =>
                    {
                        // =========================================
                        // ADMIN AUTHORIZATION
                        // =========================================

                        if (!await IsAdminAsync(actor, ct))
                            return null;

                        // =========================================
                        // REPORT OWNERSHIP
                        // =========================================

                        var hasClaim =
                            await repository.HasClaimedReportAsync(
                                reportId,
                                actor,
                                ct);

                        if (!hasClaim)
                            return null;

                        // =========================================
                        // FILE METADATA
                        // =========================================

                        var file =
                            await repository.GetEvidenceStoredFileAsync(
                                reportId,
                                evidenceId,
                                ct);

                        if (file is null)
                            return null;

                        // =========================================
                        // OPEN FILE
                        // =========================================

                        try
                        {
                            openedStream =
                                storage.OpenRead(file.StorageKey);
                        }
                        catch (FileNotFoundException)
                        {
                            return null;
                        }

                        // =========================================
                        // AUDIT FILE ACCESS
                        // =========================================

                        AddAudit(
                            actor,
                            "ReadReportEvidenceFile",
                            "Report",
                            reportId,
                            evidenceId.ToString());

                        await repository.SaveChangesAsync(ct);

                        return new FileRead(
                            openedStream,
                            file.ContentType,
                            file.OriginalFileName);
                    },
                    ct);

            if (result is null)
            {
                openedStream?.Dispose();
            }

            return result;
        }
        catch
        {
            // Prevent leaking a file stream if committing
            // the audit log or another operation fails.
            openedStream?.Dispose();
            throw;
        }
    }

    // =====================================================
    // RESOLVE / DISMISS REPORT
    // =====================================================

    public Task<bool> ResolveAsync(
        Guid actor,
        Guid reportId,
        string status,
        string note,
        CancellationToken ct)
    {
        if (status is not ("Resolved" or "Dismissed") ||
            string.IsNullOrWhiteSpace(note) ||
            note.Length > 1000)
        {
            return Task.FromResult(false);
        }

        return repository.ExecuteMutationAsync(
            async () =>
            {
                if (!await IsAdminAsync(actor, ct))
                    return false;

                // =================================================
                // ADMIN MUST OWN THE PENDING REPORT
                // =================================================

                var report =
                    await repository.GetPendingClaimedReportAsync(
                        reportId,
                        actor,
                        ct);

                if (report is null)
                    return false;

                var now = Now;

                // =================================================
                // UPDATE REPORT STATUS
                // =================================================

                report.Status = status;
                report.ReviewedAt = now;

                // =================================================
                // UPDATE EVIDENCE RETENTION
                // =================================================

                var evidence =
                    await repository.GetReportEvidenceForUpdateAsync(
                        reportId,
                        ct);

                foreach (var item in evidence)
                {
                    item.RetainUntil = now.AddDays(90);
                }

                // =================================================
                // AUDIT
                // =================================================

                AddAudit(
                    actor,
                    "ResolveReport",
                    "Report",
                    reportId,
                    note.Trim());

                await repository.SaveChangesAsync(ct);

                return true;
            },
            ct);
    }

    // =====================================================
    // WARN USER
    // =====================================================

    public async Task<bool> WarnAsync(
        Guid actor,
        Guid targetId,
        string reason,
        CancellationToken ct)
    {
        if (!ValidReason(reason))
            return false;

        var success =
            await repository.ExecuteMutationAsync(
                async () =>
                {
                    // =============================================
                    // ADMIN AUTHORIZATION
                    // =============================================

                    if (!await IsAdminAsync(actor, ct))
                        return false;

                    if (actor == targetId)
                        return false;

                    var exists =
                        await repository.UserExistsAsync(
                            targetId,
                            ct);

                    if (!exists)
                        return false;

                    var now = Now;

                    // =============================================
                    // WARNING AUDIT
                    // =============================================

                    var audit = new AdminAuditLog
                    {
                        Id = Guid.NewGuid(),
                        AdminUserId = actor,
                        Action = "WarnUser",
                        TargetType = "User",
                        TargetId = targetId,
                        Details = reason.Trim(),
                        CreatedAt = now
                    };

                    repository.AddAuditLog(audit);

                    // =============================================
                    // USER NOTIFICATION
                    // =============================================

                    repository.AddNotification(
                        new Notification
                        {
                            Id = Guid.NewGuid(),
                            RecipientUserId = targetId,
                            ActorUserId = actor,
                            ReferenceId = audit.Id,
                            Type = "AdminWarning",
                            CreatedAt = now
                        });

                    await repository.SaveChangesAsync(ct);

                    return true;
                },
                ct);

        if (!success)
            return false;

        // Realtime only after audit and notification commit.
        await events.SendAsync(
            [targetId],
            "NotificationCreated",
            new
            {
                eventId = Guid.NewGuid()
            },
            ct);

        return true;
    }

    // =====================================================
    // SUSPEND USER
    // =====================================================

    public async Task<bool> SuspendAsync(
        Guid actor,
        Guid targetId,
        string reason,
        CancellationToken ct,
        DateTime? expiresAt = null)
    {
        if (!ValidReason(reason) ||
            actor == targetId ||
            expiresAt <= Now)
        {
            return false;
        }

        var success =
            await repository.ExecuteMutationAsync(
                async () =>
                {
                    // =============================================
                    // ADMIN AUTHORIZATION
                    // =============================================

                    if (!await IsAdminAsync(actor, ct))
                        return false;

                    // =============================================
                    // LOCK TARGET USER
                    // =============================================

                    var target =
                        await repository.GetUserForUpdateAsync(
                            targetId,
                            ct);

                    if (target is null ||
                        target.SystemRole == "Admin")
                    {
                        return false;
                    }

                    // =============================================
                    // CHECK EXISTING SUSPENSION
                    // =============================================

                    var now = Now;

                    var suspended =
                        await repository.HasActiveSuspensionAsync(
                            targetId,
                            now,
                            ct);

                    if (suspended)
                        return false;

                    // =============================================
                    // CREATE SUSPENSION
                    // =============================================

                    repository.AddSuspension(
                        new AccountSuspension
                        {
                            Id = Guid.NewGuid(),
                            UserId = targetId,
                            AdminId = actor,
                            Reason = reason.Trim(),
                            Status = "Active",
                            StartedAt = now,
                            ExpiresAt = expiresAt
                        });

                    // =============================================
                    // REVOKE ALL TARGET SESSIONS
                    // =============================================

                    await repository.RevokeActiveSessionsAsync(
                        targetId,
                        now,
                        ct);

                    // =============================================
                    // AUDIT
                    // =============================================

                    AddAudit(
                        actor,
                        "SuspendUser",
                        "User",
                        targetId,
                        reason.Trim());

                    await repository.SaveChangesAsync(ct);

                    return true;
                },
                ct,
                enableRetry: true);

        if (!success)
            return false;

        // =====================================================
        // REALTIME / CONNECTION REVOCATION AFTER COMMIT
        // =====================================================

        connections.Revoke(targetId);

        await events.SendAsync(
            [targetId],
            "SessionRevoked",
            new
            {
                eventId = Guid.NewGuid()
            },
            ct);

        return true;
    }

    // =====================================================
    // UNSUSPEND USER
    // =====================================================

    public Task<bool> UnsuspendAsync(
        Guid actor,
        Guid targetId,
        string reason,
        CancellationToken ct)
    {
        if (!ValidReason(reason))
            return Task.FromResult(false);

        return repository.ExecuteMutationAsync(
            async () =>
            {
                if (!await IsAdminAsync(actor, ct))
                    return false;

                // =================================================
                // LATEST ACTIVE SUSPENSION
                // =================================================

                var suspension =
                    await repository.GetLatestActiveSuspensionAsync(
                        targetId,
                        ct);

                if (suspension is null)
                    return false;

                // =================================================
                // REVOKE SUSPENSION
                // =================================================

                suspension.RevokedAt = Now;
                suspension.Status = "Revoked";

                // =================================================
                // AUDIT
                // =================================================

                AddAudit(
                    actor,
                    "UnsuspendUser",
                    "User",
                    targetId,
                    reason.Trim());

                await repository.SaveChangesAsync(ct);

                return true;
            },
            ct);
    }

    // =====================================================
    // DISABLE GROUP
    // =====================================================

    public async Task<bool> DisableGroupAsync(
        Guid actor,
        Guid groupId,
        string reason,
        CancellationToken ct)
    {
        if (!ValidReason(reason))
            return false;

        var success =
            await repository.ExecuteMutationAsync(
                async () =>
                {
                    if (!await IsAdminAsync(actor, ct))
                        return false;

                    // =============================================
                    // GET ACTIVE GROUP
                    // =============================================

                    var group =
                        await repository.GetActiveGroupAsync(
                            groupId,
                            ct);

                    if (group is null)
                        return false;

                    // =============================================
                    // DISABLE GROUP
                    // =============================================

                    group.Status = "Disabled";
                    group.UpdatedAt = Now;

                    // =============================================
                    // AUDIT
                    // =============================================

                    AddAudit(
                        actor,
                        "DisableGroup",
                        "Group",
                        groupId,
                        reason.Trim());

                    await repository.SaveChangesAsync(ct);

                    return true;
                },
                ct);

        if (!success)
            return false;

        // Inform clients only after commit.
        await events.GroupChangedAsync(
            groupId,
            ct);

        return true;
    }

    // =====================================================
    // LIST APPEALS
    // =====================================================

    public async Task<List<AppealDto>?> AppealsAsync(
        Guid actor,
        CancellationToken ct,
        int skip = 0)
    {
        if (!await IsAdminAsync(actor, ct))
            return null;

        return await repository.GetAppealsAsync(
            Math.Max(0, skip),
            ct);
    }

    // =====================================================
    // REVIEW APPEAL
    // =====================================================

    public Task<bool> ReviewAppealAsync(
        Guid actor,
        Guid appealId,
        string status,
        string note,
        CancellationToken ct)
    {
        if (status is not ("Accepted" or "Rejected") ||
            !ValidReason(note))
        {
            return Task.FromResult(false);
        }

        return repository.ExecuteMutationAsync(
            async () =>
            {
                if (!await IsAdminAsync(actor, ct))
                    return false;

                // =================================================
                // PENDING APPEAL
                // =================================================

                var appeal =
                    await repository.GetPendingAppealAsync(
                        appealId,
                        ct);

                if (appeal is null)
                    return false;

                // =================================================
                // UPDATE APPEAL
                // =================================================

                var now = Now;

                appeal.Status = status;
                appeal.ReviewedByAdminId = actor;
                appeal.ReviewedAt = now;
                appeal.ReviewNote = note.Trim();

                // =================================================
                // AUDIT
                // =================================================

                AddAudit(
                    actor,
                    "ReviewAppeal",
                    "Appeal",
                    appealId,
                    status + ": " + note.Trim());

                await repository.SaveChangesAsync(ct);

                // Preserve original behavior:
                // accepting an appeal does not automatically
                // revoke the suspension here.
                return true;
            },
            ct);
    }

    // =====================================================
    // ADMIN AUDIT LOGS
    // =====================================================

    public async Task<List<AdminAuditLog>?> AuditAsync(
        Guid actor,
        CancellationToken ct,
        int skip = 0)
    {
        if (!await IsAdminAsync(actor, ct))
            return null;

        return await repository.GetAuditLogsAsync(
            Math.Max(0, skip),
            ct);
    }

    // =====================================================
    // CREATE AUDIT LOG
    // =====================================================

    private void AddAudit(
        Guid actor,
        string action,
        string targetType,
        Guid? targetId,
        string? details)
    {
        repository.AddAuditLog(
            new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = actor,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Details = details,
                CreatedAt = Now
            });
    }

    // =====================================================
    // VALIDATE ADMIN REASON
    // =====================================================

    private static bool ValidReason(string? reason)
    {
        return !string.IsNullOrWhiteSpace(reason) &&
               reason.Length <= 1000;
    }

    // =====================================================
    // MAP REPORT TO DTO
    // =====================================================

    private static AdminReportDto Map(Report report)
    {
        return new AdminReportDto(
            report.Id,
            report.ReporterUserId,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Status,
            report.CreatedAt,
            report.ReviewedByAdminId,
            report.ReviewedAt);
    }
}

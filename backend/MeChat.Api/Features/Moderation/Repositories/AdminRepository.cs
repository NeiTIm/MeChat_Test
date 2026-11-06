
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Moderation;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation.Repositories;

public sealed class AdminRepository(
    AppDbContext db) : IAdminRepository
{
    // =====================================================
    // MUTATION TRANSACTION
    // =====================================================

    public async Task<bool> ExecuteMutationAsync(
        Func<Task<bool>> operation,
        CancellationToken ct,
        bool enableRetry = false)
    {
        var maxAttempts = enableRetry ? 3 : 1;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
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
                when (enableRetry &&
                      attempt < maxAttempts - 1 &&
                      IsRetryable(error))
            {
                db.ChangeTracker.Clear();

                await Task.Delay(
                    40 * (attempt + 1),
                    ct);
            }
            catch (Exception error)
                when (enableRetry && IsRetryable(error))
            {
                db.ChangeTracker.Clear();
                return false;
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }

        return false;
    }

    // =====================================================
    // AUDITED READ TRANSACTION
    // =====================================================

    public async Task<T?> ExecuteAuditedReadAsync<T>(
        Func<Task<T?>> operation,
        CancellationToken ct)
        where T : class
    {
        try
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    ct);

            var result = await operation();

            if (result is null)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return null;
            }

            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // ADMIN AUTHORIZATION
    // =====================================================

    public Task<bool> IsAdminAsync(
        Guid actor,
        DateTime now,
        CancellationToken ct)
    {
        return db.Users.AsNoTracking().AnyAsync(
            user =>
                user.Id == actor &&
                user.SystemRole == "Admin" &&
                user.EmailVerifiedAt != null &&
                user.DeactivatedAt == null &&
                !db.AccountSuspensions.Any(suspension =>
                    suspension.UserId == actor &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (suspension.ExpiresAt == null ||
                     suspension.ExpiresAt > now)),
            ct);
    }

    // =====================================================
    // DASHBOARD
    // =====================================================

    public async Task<AdminDashboardDto> GetDashboardAsync(
        DateTime start,
        CancellationToken ct)
    {
        var newUsers = await db.Users
            .AsNoTracking()
            .Where(user => user.CreatedAt >= start)
            .GroupBy(user => user.CreatedAt.Date)
            .Select(group => new
            {
                Date = group.Key,
                Count = group.Count()
            })
            .ToListAsync(ct);

        var newMessages = await db.Messages
            .AsNoTracking()
            .Where(message => message.CreatedAt >= start)
            .GroupBy(message => message.CreatedAt.Date)
            .Select(group => new
            {
                Date = group.Key,
                Count = group.Count()
            })
            .ToListAsync(ct);

        var activity = Enumerable.Range(0, 7)
            .Select(offset => start.AddDays(offset))
            .Select(date => new AdminActivityDto(
                date,
                newUsers.FirstOrDefault(
                    item => item.Date == date)?.Count ?? 0,
                newMessages.FirstOrDefault(
                    item => item.Date == date)?.Count ?? 0))
            .ToList();

        var users = await db.Users.CountAsync(ct);

        var messages = await db.Messages.CountAsync(ct);

        var groups = await db.Conversations.CountAsync(
            conversation => conversation.Type == "Group",
            ct);

        var pendingReports = await db.Reports.CountAsync(
            report => report.Status == "Pending",
            ct);

        return new AdminDashboardDto(
            users,
            messages,
            groups,
            pendingReports,
            activity);
    }

    // =====================================================
    // LIST USERS
    // =====================================================

    public Task<List<AdminUserDto>> GetUsersAsync(
        int skip,
        DateTime now,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip(Math.Max(0, skip))
            .Take(100)
            .Select(user => new AdminUserDto(
                user.Id,
                user.Email,
                user.Username,
                user.SystemRole,
                db.AccountSuspensions.Any(suspension =>
                    suspension.UserId == user.Id &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (suspension.ExpiresAt == null ||
                     suspension.ExpiresAt > now)),
                user.CreatedAt))
            .ToListAsync(ct);
    }

    // =====================================================
    // LIST GROUPS
    // =====================================================

    public Task<List<AdminGroupDto>> GetGroupsAsync(
        int skip,
        CancellationToken ct)
    {
        return (
            from profile in db.GroupProfiles.AsNoTracking()
            join conversation in db.Conversations.AsNoTracking()
                on profile.ConversationId equals conversation.Id

            orderby conversation.UpdatedAt descending,
                    conversation.Id

            select new AdminGroupDto(
                conversation.Id,
                profile.Name,
                conversation.Status,
                db.ConversationMembers.Count(member =>
                    member.ConversationId == conversation.Id &&
                    member.LeftAt == null))
        )
        .Skip(Math.Max(0, skip))
        .Take(100)
        .ToListAsync(ct);
    }

    // =====================================================
    // LIST REPORTS
    // =====================================================

    public Task<List<AdminReportDto>> GetReportsAsync(
        int skip,
        CancellationToken ct)
    {
        return db.Reports
            .AsNoTracking()
            .OrderByDescending(report => report.CreatedAt)
            .ThenBy(report => report.Id)
            .Skip(Math.Max(0, skip))
            .Take(100)
            .Select(report => new AdminReportDto(
                report.Id,
                report.ReporterUserId,
                report.TargetType,
                report.TargetId,
                report.Reason,
                report.Status,
                report.CreatedAt,
                report.ReviewedByAdminId,
                report.ReviewedAt))
            .ToListAsync(ct);
    }

    // =====================================================
    // CLAIM REPORT WITH SQL SERVER LOCK
    // =====================================================

    public Task<Report?> GetReportForUpdateAsync(
        Guid reportId,
        CancellationToken ct)
    {
        return db.Reports
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [Reports] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {reportId}
                """)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // REPORT OWNED BY ADMIN
    // =====================================================

    public Task<Report?> GetClaimedReportAsync(
        Guid reportId,
        Guid adminId,
        CancellationToken ct)
    {
        return db.Reports.SingleOrDefaultAsync(
            report =>
                report.Id == reportId &&
                report.ReviewedByAdminId == adminId,
            ct);
    }

    public Task<Report?> GetPendingClaimedReportAsync(
        Guid reportId,
        Guid adminId,
        CancellationToken ct)
    {
        return db.Reports.SingleOrDefaultAsync(
            report =>
                report.Id == reportId &&
                report.ReviewedByAdminId == adminId &&
                report.Status == "Pending",
            ct);
    }

    public Task<bool> HasClaimedReportAsync(
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
    // REPORT EVIDENCE
    // =====================================================

    public Task<List<AdminEvidenceDto>> GetReportEvidenceAsync(
        Guid reportId,
        CancellationToken ct)
    {
        return db.ReportEvidences
            .AsNoTracking()
            .Where(evidence =>
                evidence.ReportId == reportId)
            .Select(evidence => new AdminEvidenceDto(
                evidence.Id,
                evidence.SourceMessageId,
                evidence.EvidenceContent,
                evidence.StoredFileId,
                evidence.CapturedAt,
                evidence.RetainUntil,
                evidence.LegalHold))
            .ToListAsync(ct);
    }

    public Task<List<ReportEvidence>>
        GetReportEvidenceForUpdateAsync(
            Guid reportId,
            CancellationToken ct)
    {
        return db.ReportEvidences
            .Where(evidence =>
                evidence.ReportId == reportId)
            .ToListAsync(ct);
    }

    public Task<StoredFile?> GetEvidenceStoredFileAsync(
        Guid reportId,
        Guid evidenceId,
        CancellationToken ct)
    {
        return (
            from evidence in db.ReportEvidences.AsNoTracking()
            join stored in db.StoredFiles.AsNoTracking()
                on evidence.StoredFileId equals stored.Id

            where evidence.Id == evidenceId &&
                  evidence.ReportId == reportId &&
                  stored.DeletedAt == null

            select stored
        ).SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // USERS
    // =====================================================

    public Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users.AnyAsync(
            user => user.Id == userId,
            ct);
    }

    public Task<User?> GetUserForUpdateAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [Users] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {userId}
                """)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // SUSPENSIONS
    // =====================================================

    public Task<bool> HasActiveSuspensionAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.AccountSuspensions.AnyAsync(
            suspension =>
                suspension.UserId == userId &&
                suspension.Status == "Active" &&
                suspension.RevokedAt == null &&
                (suspension.ExpiresAt == null ||
                 suspension.ExpiresAt > now),
            ct);
    }

    public Task<AccountSuspension?>
        GetLatestActiveSuspensionAsync(
            Guid userId,
            CancellationToken ct)
    {
        return db.AccountSuspensions
            .Where(suspension =>
                suspension.UserId == userId &&
                suspension.Status == "Active" &&
                suspension.RevokedAt == null)
            .OrderByDescending(
                suspension => suspension.StartedAt)
            .FirstOrDefaultAsync(ct);
    }

    public void AddSuspension(
        AccountSuspension suspension)
    {
        db.AccountSuspensions.Add(suspension);
    }

    // =====================================================
    // REVOKE USER SESSIONS
    // =====================================================

    public Task<int> RevokeActiveSessionsAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.UserSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    now),
                ct);
    }

    // =====================================================
    // GROUP
    // =====================================================

    public Task<Conversation?> GetActiveGroupAsync(
        Guid groupId,
        CancellationToken ct)
    {
        return db.Conversations.SingleOrDefaultAsync(
            conversation =>
                conversation.Id == groupId &&
                conversation.Type == "Group" &&
                conversation.Status == "Active",
            ct);
    }

    // =====================================================
    // APPEALS
    // =====================================================

    public Task<List<AppealDto>> GetAppealsAsync(
        int skip,
        CancellationToken ct)
    {
        return db.AccountAppeals
            .AsNoTracking()
            .OrderByDescending(appeal => appeal.CreatedAt)
            .ThenBy(appeal => appeal.Id)
            .Skip(Math.Max(0, skip))
            .Take(100)
            .Select(appeal => new AppealDto(
                appeal.Id,
                appeal.SuspensionId,
                appeal.Reason,
                appeal.Status,
                appeal.CreatedAt,
                appeal.ReviewedAt,
                appeal.ReviewNote,
                appeal.UserId,
                db.Users
                    .Where(user => user.Id == appeal.UserId)
                    .Select(user => user.Username)
                    .FirstOrDefault()))
            .ToListAsync(ct);
    }

    public Task<AccountAppeal?> GetPendingAppealAsync(
        Guid appealId,
        CancellationToken ct)
    {
        return db.AccountAppeals.SingleOrDefaultAsync(
            appeal =>
                appeal.Id == appealId &&
                appeal.Status == "Pending",
            ct);
    }

    // =====================================================
    // AUDIT LOGS
    // =====================================================

    public Task<List<AdminAuditLog>> GetAuditLogsAsync(
        int skip,
        CancellationToken ct)
    {
        return db.AdminAuditLogs
            .AsNoTracking()
            .OrderByDescending(audit => audit.CreatedAt)
            .ThenBy(audit => audit.Id)
            .Skip(Math.Max(0, skip))
            .Take(100)
            .ToListAsync(ct);
    }

    public void AddAuditLog(
        AdminAuditLog audit)
    {
        db.AdminAuditLogs.Add(audit);
    }

    // =====================================================
    // NOTIFICATIONS
    // =====================================================

    public void AddNotification(
        Notification notification)
    {
        db.Notifications.Add(notification);
    }

    // =====================================================
    // SQL SERVER RETRYABLE CONFLICTS
    // =====================================================

    private static bool IsRetryable(Exception error)
    {
        for (Exception? current = error;
             current is not null;
             current = current.InnerException)
        {
            if (current is SqlException sql &&
                sql.Number is 1205 or 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}

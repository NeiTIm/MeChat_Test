
using MeChat.Api.Features.Moderation;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation.Repositories.Interfaces;

public interface IAdminRepository
{
    // =====================================================
    // TRANSACTIONS / PERSISTENCE
    // =====================================================

    Task<bool> ExecuteMutationAsync(
        Func<Task<bool>> operation,
        CancellationToken ct,
        bool enableRetry = false);

    Task<T?> ExecuteAuditedReadAsync<T>(
        Func<Task<T?>> operation,
        CancellationToken ct)
        where T : class;

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // AUTHORIZATION
    // =====================================================

    Task<bool> IsAdminAsync(
        Guid actor,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // DASHBOARD
    // =====================================================

    Task<AdminDashboardDto> GetDashboardAsync(
        DateTime start,
        CancellationToken ct);

    // =====================================================
    // ADMIN LISTS
    // =====================================================

    Task<List<AdminUserDto>> GetUsersAsync(
        int skip,
        DateTime now,
        CancellationToken ct);

    Task<List<AdminGroupDto>> GetGroupsAsync(
        int skip,
        CancellationToken ct);

    Task<List<AdminReportDto>> GetReportsAsync(
        int skip,
        CancellationToken ct);

    Task<List<AppealDto>> GetAppealsAsync(
        int skip,
        CancellationToken ct);

    Task<List<AdminAuditLog>> GetAuditLogsAsync(
        int skip,
        CancellationToken ct);

    // =====================================================
    // REPORTS
    // =====================================================

    Task<Report?> GetReportForUpdateAsync(
        Guid reportId,
        CancellationToken ct);

    Task<Report?> GetClaimedReportAsync(
        Guid reportId,
        Guid adminId,
        CancellationToken ct);

    Task<Report?> GetPendingClaimedReportAsync(
        Guid reportId,
        Guid adminId,
        CancellationToken ct);

    Task<bool> HasClaimedReportAsync(
        Guid reportId,
        Guid adminId,
        CancellationToken ct);

    Task<List<AdminEvidenceDto>> GetReportEvidenceAsync(
        Guid reportId,
        CancellationToken ct);

    Task<List<ReportEvidence>> GetReportEvidenceForUpdateAsync(
        Guid reportId,
        CancellationToken ct);

    Task<StoredFile?> GetEvidenceStoredFileAsync(
        Guid reportId,
        Guid evidenceId,
        CancellationToken ct);

    // =====================================================
    // USERS / SUSPENSIONS / SESSIONS
    // =====================================================

    Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken ct);

    Task<User?> GetUserForUpdateAsync(
        Guid userId,
        CancellationToken ct);

    Task<bool> HasActiveSuspensionAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    Task<AccountSuspension?> GetLatestActiveSuspensionAsync(
        Guid userId,
        CancellationToken ct);

    void AddSuspension(
        AccountSuspension suspension);

    Task<int> RevokeActiveSessionsAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // GROUPS
    // =====================================================

    Task<Conversation?> GetActiveGroupAsync(
        Guid groupId,
        CancellationToken ct);

    // =====================================================
    // APPEALS
    // =====================================================

    Task<AccountAppeal?> GetPendingAppealAsync(
        Guid appealId,
        CancellationToken ct);

    // =====================================================
    // AUDIT / NOTIFICATIONS
    // =====================================================

    void AddAuditLog(
        AdminAuditLog audit);

    void AddNotification(
        Notification notification);
}

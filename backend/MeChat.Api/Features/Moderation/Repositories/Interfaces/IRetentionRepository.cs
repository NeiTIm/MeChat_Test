
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation.Repositories.Interfaces;

public interface IRetentionRepository
{
    // =====================================================
    // PREVIEW / DRY RUN
    // =====================================================

    Task<RetentionPreviewDto> GetPreviewAsync(
        DateTime now,
        CancellationToken ct);

    Task<List<RetentionPlanItem>> GetPlanItemsAsync(
        string kind,
        DateTime now,
        int offset,
        int take,
        CancellationToken ct);

    // =====================================================
    // LEGAL HOLD
    // =====================================================

    Task<bool> ExecuteHoldAtomicAsync(
        Func<Task<bool>> operation,
        CancellationToken ct);

    Task<ReportEvidence?> GetEvidenceForUpdateAsync(
        Guid evidenceId,
        CancellationToken ct);

    Task<AdminAuditLog?> GetAuditForUpdateAsync(
        Guid auditId,
        CancellationToken ct);

    Task<bool> IsReportAssignedToAdminAsync(
        Guid reportId,
        Guid adminId,
        CancellationToken ct);

    // =====================================================
    // PURGE ELIGIBILITY (SHARED WITH MAINTENANCE)
    // =====================================================

    Task<List<Guid>> GetEligibleEvidenceIdsAsync(
        DateTime now,
        int batchSize,
        CancellationToken ct);

    Task<int> DeleteEligibleEvidenceAsync(
        DateTime now,
        IReadOnlyCollection<Guid> evidenceIds,
        CancellationToken ct);

    // =====================================================
    // AUDIT / PERSISTENCE
    // =====================================================

    void AddAuditLog(AdminAuditLog audit);

    Task<int> SaveChangesAsync(
        CancellationToken ct);
}


using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation.Repositories.Interfaces;

public interface IReportRepository
{
    // =====================================================
    // TRANSACTION / PERSISTENCE
    // =====================================================

    Task<T?> ExecuteAtomicAsync<T>(
        Func<Task<T?>> operation,
        CancellationToken ct)
        where T : class;

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // MESSAGES
    // =====================================================

    Task<Message?> GetUnrecalledMessageAsync(
        Guid messageId,
        CancellationToken ct);

    Task<List<Guid>> GetMessageAttachmentFileIdsAsync(
        Guid messageId,
        CancellationToken ct);

    // =====================================================
    // USERS
    // =====================================================

    Task<bool> IsVerifiedUserAsync(
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // REPORTS
    // =====================================================

    void AddReport(Report report);

    Task<List<ReportDto>> GetUserReportsAsync(
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // REPORT EVIDENCE
    // =====================================================

    void AddEvidence(ReportEvidence evidence);

    void AddEvidenceRange(
        IEnumerable<ReportEvidence> evidence);
}

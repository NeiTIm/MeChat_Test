
using System.Data;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation.Repositories;

public sealed class ReportRepository(
    AppDbContext db) : IReportRepository
{
    // =====================================================
    // SERIALIZABLE TRANSACTION
    // =====================================================

    public async Task<T?> ExecuteAtomicAsync<T>(
        Func<Task<T?>> operation,
        CancellationToken ct)
        where T : class
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        try
        {
            var result = await operation();

            // Business validation failed.
            // Roll back any changes made in this transaction.
            if (result is null)
            {
                await transaction.RollbackAsync(ct);

                db.ChangeTracker.Clear();

                return null;
            }

            // Commit report and evidence together.
            await transaction.CommitAsync(ct);

            return result;
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

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // GET UNRECALLED MESSAGE
    // =====================================================

    public Task<Message?> GetUnrecalledMessageAsync(
        Guid messageId,
        CancellationToken ct)
    {
        // Keep tracking behavior compatible with the
        // original service inside the transaction.

        return db.Messages.SingleOrDefaultAsync(
            message =>
                message.Id == messageId &&
                message.RecalledAt == null,
            ct);
    }

    // =====================================================
    // GET MESSAGE ATTACHMENTS
    // =====================================================

    public Task<List<Guid>> GetMessageAttachmentFileIdsAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.MessageAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.MessageId == messageId)
            .Select(attachment =>
                attachment.StoredFileId)
            .ToListAsync(ct);
    }

    // =====================================================
    // VERIFIED USER
    // =====================================================

    public Task<bool> IsVerifiedUserAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.Id == userId &&
                    user.EmailVerifiedAt != null,
                ct);
    }

    // =====================================================
    // ADD REPORT
    // =====================================================

    public void AddReport(
        Report report)
    {
        db.Reports.Add(report);
    }

    // =====================================================
    // ADD REPORT EVIDENCE
    // =====================================================

    public void AddEvidence(
        ReportEvidence evidence)
    {
        db.ReportEvidences.Add(evidence);
    }

    // =====================================================
    // ADD MULTIPLE REPORT EVIDENCE RECORDS
    // =====================================================

    public void AddEvidenceRange(
        IEnumerable<ReportEvidence> evidence)
    {
        db.ReportEvidences.AddRange(evidence);
    }

    // =====================================================
    // GET MY REPORTS
    // =====================================================

    public Task<List<ReportDto>> GetUserReportsAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Reports
            .AsNoTracking()
            .Where(report =>
                report.ReporterUserId == userId)
            .OrderByDescending(report =>
                report.CreatedAt)
            .Select(report => new ReportDto(
                report.Id,
                report.TargetType,
                report.TargetId,
                report.Reason,
                report.Status,
                report.CreatedAt,
                report.ReviewedAt))
            .ToListAsync(ct);
    }
}

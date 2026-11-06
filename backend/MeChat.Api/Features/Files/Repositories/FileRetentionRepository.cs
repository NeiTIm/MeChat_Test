
using System.Data;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Files.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Files.Repositories;

public sealed class FileRetentionRepository(
    AppDbContext db) : IFileRetentionRepository
{
    // =====================================================
    // ELIGIBILITY QUERY
    // =====================================================

    private IQueryable<StoredFile> Eligible(
        DateTime now,
        FileRetentionOptions options)
    {
        var pendingDeadline =
            now.AddHours(-options.PendingHours);

        var avatarDeadline =
            now.AddDays(-options.AvatarDays);

        var recalledDeadline =
            now.AddDays(-options.RecalledDays);

        return db.StoredFiles.Where(file =>
            file.DeletedAt == null &&

            // =============================================
            // NO USER AVATAR REFERENCE
            // =============================================

            !db.Users.Any(user =>
                user.AvatarFileId == file.Id) &&

            // =============================================
            // NO GROUP AVATAR REFERENCE
            // =============================================

            !db.GroupProfiles.Any(group =>
                group.AvatarFileId == file.Id) &&

            // =============================================
            // NO REPORT EVIDENCE REFERENCE
            // =============================================

            !db.ReportEvidences.Any(evidence =>
                evidence.StoredFileId == file.Id) &&

            // =============================================
            // NO ACTIVE MESSAGE REFERENCE
            // =============================================

            !db.MessageAttachments.Any(attachment =>
                attachment.StoredFileId == file.Id &&
                db.Messages.Any(message =>
                    message.Id == attachment.MessageId &&
                    message.RecalledAt == null)) &&

            // =============================================
            // RETENTION CONDITIONS
            // =============================================

            (
                // Pending upload expired and unreferenced.
                (
                    file.Status == "Pending" &&
                    file.CreatedAt <= pendingDeadline &&
                    !db.MessageAttachments.Any(attachment =>
                        attachment.StoredFileId == file.Id)
                )

                ||

                // Unreferenced avatar expired.
                (
                    file.Status == "Avatar" &&
                    file.CreatedAt <= avatarDeadline &&
                    !db.MessageAttachments.Any(attachment =>
                        attachment.StoredFileId == file.Id)
                )

                ||

                // Attached file: every referenced message
                // must have been recalled long enough ago.
                (
                    file.Status == "Attached" &&

                    db.MessageAttachments.Any(attachment =>
                        attachment.StoredFileId == file.Id) &&

                    !db.MessageAttachments.Any(attachment =>
                        attachment.StoredFileId == file.Id &&
                        db.Messages.Any(message =>
                            message.Id == attachment.MessageId &&
                            (
                                message.RecalledAt == null ||
                                message.RecalledAt >
                                    recalledDeadline
                            )))
                )
            ));
    }

    // =====================================================
    // VALIDATE DATABASE TARGET
    // =====================================================

    public async Task ValidateDatabaseTargetAsync(
        CancellationToken ct)
    {
        // Validate the configured SQL Server target.
        DatabaseTarget.Validate(
            db.Database.GetConnectionString());

        var connectionWasOpen =
            db.Database.GetDbConnection().State ==
            ConnectionState.Open;

        if (!connectionWasOpen)
        {
            await db.Database.OpenConnectionAsync(ct);
        }

        try
        {
            await using var command =
                db.Database.GetDbConnection()
                    .CreateCommand();

            command.CommandText = "SELECT DB_NAME()";

            var actualDatabase =
                await command.ExecuteScalarAsync(ct)
                    as string;

            if (!string.Equals(
                    actualDatabase,
                    "MeChat",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Unexpected cleanup database target.");
            }
        }
        finally
        {
            if (!connectionWasOpen)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    // =====================================================
    // GET CLEANUP PREVIEW
    // =====================================================

    public async Task<FileCleanupPreview> GetPreviewAsync(
        DateTime now,
        FileRetentionOptions options,
        CancellationToken ct)
    {
        var rows = await Eligible(now, options)
            .AsNoTracking()
            .OrderBy(file => file.CreatedAt)
            .ThenBy(file => file.Id)
            .Take(options.BatchSize + 1)
            .Select(file => new
            {
                file.Id,
                file.Status,
                file.SizeBytes
            })
            .ToListAsync(ct);

        var candidates = rows
            .Take(options.BatchSize)
            .Select(file => new FileCleanupCandidate(
                file.Id,
                file.Status == "Pending"
                    ? "ExpiredPendingUpload"
                    : file.Status == "Avatar"
                        ? "UnreferencedAvatar"
                        : "AllMessageReferencesRecalled",
                file.SizeBytes))
            .ToList();

        return new FileCleanupPreview(
            candidates,
            rows.Count > options.BatchSize,
            options.EnableDeletion);
    }

    // =====================================================
    // MARK FILE AS CLEANUP PENDING
    // =====================================================

    public async Task<bool> MarkCleanupPendingAsync(
        Guid fileId,
        DateTime now,
        FileRetentionOptions options,
        CancellationToken ct)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        try
        {
            // =================================================
            // LOCK STORED FILE
            // =================================================

            // Update lock prevents two cleanup workers
            // from modifying the same StoredFile row
            // concurrently.
            //
            // FromSqlInterpolated parameterizes fileId.

            var file = await db.StoredFiles
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM [StoredFiles] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Id] = {fileId}
                    """)
                .SingleOrDefaultAsync(ct);

            if (file is null)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return false;
            }

            // =================================================
            // RECHECK SQL ELIGIBILITY
            // =================================================

            var eligible = await Eligible(now, options)
                .AnyAsync(item => item.Id == fileId, ct);

            if (!eligible)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return false;
            }

            // =================================================
            // RELOAD RETENTION FACTS
            // =================================================

            var references = await db.MessageAttachments
                .Where(attachment =>
                    attachment.StoredFileId == fileId)
                .Join(
                    db.Messages,
                    attachment => attachment.MessageId,
                    message => message.Id,
                    (attachment, message) => message.RecalledAt)
                .ToListAsync(ct);

            var hasUserAvatar =
                await db.Users.AnyAsync(
                    user => user.AvatarFileId == fileId,
                    ct);

            var hasGroupAvatar =
                await db.GroupProfiles.AnyAsync(
                    group => group.AvatarFileId == fileId,
                    ct);

            var hasEvidence =
                await db.ReportEvidences.AnyAsync(
                    evidence =>
                        evidence.StoredFileId == fileId,
                    ct);

            var facts = new FileRetentionFacts(
                file.Status,
                file.CreatedAt,
                hasUserAvatar || hasGroupAvatar,
                hasEvidence,
                references.Count,
                references.Any(recalled =>
                    recalled is null),
                references.Count == 0
                    ? null
                    : references.Max());

            // =================================================
            // RECHECK DOMAIN POLICY
            // =================================================

            var reason = FileRetentionPolicy.Reason(
                facts,
                now,
                options);

            if (reason is null ||
                file.DeletedAt is not null)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return false;
            }

            // =================================================
            // CREATE TOMBSTONE
            // =================================================

            file.Status = "CleanupPending";
            file.DeletedAt = now;

            // =================================================
            // SAVE AND COMMIT
            // =================================================

            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return true;
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
    // FILES AWAITING PHYSICAL CLEANUP
    // =====================================================

    public Task<List<StoredFile>>
        GetCleanupPendingFilesAsync(
            int batchSize,
            CancellationToken ct)
    {
        // Tracking is necessary because
        // FileCleanupCompletion modifies StoredFile
        // and then invokes SaveChangesAsync.

        return db.StoredFiles
            .Where(file =>
                file.Status == "CleanupPending" &&
                file.DeletedAt != null)
            .OrderBy(file => file.DeletedAt)
            .ThenBy(file => file.Id)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }
}

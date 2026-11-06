
using Microsoft.Extensions.Options;
using MeChat.Api.Features.Files.Repositories.Interfaces;

namespace MeChat.Api.Features.Files;

public sealed class FileRetentionService(
    IFileRetentionRepository repository,
    IFileStorage storage,
    TimeProvider clock,
    IOptions<FileRetentionOptions> configuration,
    ILogger<FileRetentionService> logger)
{
    private readonly FileRetentionOptions options =
        configuration.Value;

    private DateTime Now =>
        clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // PREVIEW FILE CLEANUP
    // =====================================================

    public Task<FileCleanupPreview> PreviewAsync(
        CancellationToken ct)
    {
        // Preview only reads database metadata.
        // No file deletion is performed.

        return repository.GetPreviewAsync(
            Now,
            options,
            ct);
    }

    // =====================================================
    // EXECUTE FILE CLEANUP
    // =====================================================

    public async Task<int> SweepAsync(
        CancellationToken ct)
    {
        // =================================================
        // DELETION FEATURE FLAG
        // =================================================

        if (!options.EnableDeletion)
        {
            return 0;
        }

        // =================================================
        // APPROVAL VALIDATION
        // =================================================

        if (string.IsNullOrWhiteSpace(
                options.ApprovalReference))
        {
            throw new InvalidOperationException(
                "File deletion requires an approved operation reference.");
        }

        // =================================================
        // BATCH VALIDATION
        // =================================================

        if (options.BatchSize is < 1 or > 500)
        {
            throw new InvalidOperationException(
                "File retention batch size must be between 1 and 500.");
        }

        // =================================================
        // DATABASE SAFETY
        // =================================================

        await repository.ValidateDatabaseTargetAsync(
            ct);

        // =================================================
        // PREVIEW ELIGIBLE FILES
        // =================================================

        var preview = await PreviewAsync(ct);

        // =================================================
        // MARK FILES FOR CLEANUP
        // =================================================

        foreach (var candidate in preview.Items)
        {
            ct.ThrowIfCancellationRequested();

            // =============================================
            // SERIALIZABLE ELIGIBILITY RECHECK
            // =============================================

            // Repository independently checks:
            //
            // - File is not deleted
            // - Retention period has expired
            // - No active avatar reference
            // - No report evidence reference
            // - No active message reference
            // - FileRetentionPolicy permits cleanup
            //
            // It commits CleanupPending before returning.

            await repository.MarkCleanupPendingAsync(
                candidate.Id,
                Now,
                options,
                ct);
        }

        // =================================================
        // RESUME CLEANUP PENDING FILES
        // =================================================

        // Also process files marked in previous runs.
        //
        // This is intentional: process interruption
        // may happen after the database tombstone was
        // committed but before physical deletion.

        var pending =
            await repository.GetCleanupPendingFilesAsync(
                options.BatchSize,
                ct);

        var removed = 0;

        // =================================================
        // COMPLETE PHYSICAL FILE CLEANUP
        // =================================================

        foreach (var file in pending)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // =============================================
                // EXISTING CLEANUP COMPLETION
                // =============================================

                // Preserve the existing helper responsible
                // for deleting the physical file and
                // updating cleanup status.
                //
                // FileRetentionRepository.SaveChangesAsync
                // uses the same scoped AppDbContext that
                // tracked the StoredFile entity.

                await FileCleanupCompletion.CompleteAsync(
                    file,
                    storage,
                    token =>
                        repository.SaveChangesAsync(token),
                    ct);

                removed++;

                // =============================================
                // LOG SUCCESS
                // =============================================

                logger.LogInformation(
                    "File cleanup completed for {FileId}; approval {Approval}",
                    file.Id,
                    options.ApprovalReference);
            }
            catch (IOException error)
            {
                // =============================================
                // RETRY ON NEXT SWEEP
                // =============================================

                logger.LogWarning(
                    error,
                    "File cleanup will retry {FileId}",
                    file.Id);
            }
        }

        // =================================================
        // RETURN COMPLETED FILE COUNT
        // =================================================

        return removed;
    }
}

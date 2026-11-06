
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Files;

/// <summary>
/// Completes physical file cleanup after the SQL tombstone
/// has been successfully committed.
/// </summary>
/// <remarks>
/// Storage deletion must be idempotent.
/// This operation does not create its own database transaction.
/// </remarks>
public static class FileCleanupCompletion
{
    // =====================================================
    // COMPLETE PHYSICAL FILE CLEANUP
    // =====================================================

    public static async Task CompleteAsync(
        StoredFile tombstone,
        IFileStorage storage,
        Func<CancellationToken, Task> persist,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE ARGUMENTS
        // =================================================

        ArgumentNullException.ThrowIfNull(tombstone);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(persist);

        // =================================================
        // REQUIRE COMMITTED TOMBSTONE
        // =================================================

        // Only files previously marked as CleanupPending
        // and assigned a DeletedAt timestamp may proceed.
        //
        // The caller is responsible for ensuring that
        // these values have already been committed to SQL.

        if (tombstone.Status != "CleanupPending" ||
            tombstone.DeletedAt is null)
        {
            throw new InvalidOperationException(
                "A committed cleanup tombstone is required.");
        }

        // =================================================
        // CHECK CANCELLATION
        // =================================================

        ct.ThrowIfCancellationRequested();

        // =================================================
        // DELETE PHYSICAL FILE
        // =================================================

        // Storage.Delete must be idempotent:
        // deleting an already missing file should succeed.
        //
        // If this throws, SQL status remains CleanupPending
        // so a future sweep can retry.

        storage.Delete(tombstone.StorageKey);

        // =================================================
        // MARK SQL METADATA AS DELETED
        // =================================================

        tombstone.Status = "Deleted";

        // =================================================
        // PERSIST FINAL STATUS
        // =================================================

        try
        {
            await persist(ct);
        }
        catch
        {
            // Restore in-memory tracking state so that
            // the file can be retried after persistence
            // failed.
            //
            // The committed SQL tombstone is still
            // CleanupPending if this save did not commit.

            tombstone.Status = "CleanupPending";

            throw;
        }
    }
}

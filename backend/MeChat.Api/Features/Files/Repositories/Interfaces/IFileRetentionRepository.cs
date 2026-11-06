
namespace MeChat.Api.Features.Files.Repositories.Interfaces;

public interface IFileRetentionRepository
{
    // =====================================================
    // DATABASE SAFETY
    // =====================================================

    Task ValidateDatabaseTargetAsync(
        CancellationToken ct);

    // =====================================================
    // PREVIEW
    // =====================================================

    Task<FileCleanupPreview> GetPreviewAsync(
        DateTime now,
        FileRetentionOptions options,
        CancellationToken ct);

    // =====================================================
    // MARK FOR CLEANUP
    // =====================================================

    // Recheck eligibility inside a Serializable
    // transaction before changing the file status.
    Task<bool> MarkCleanupPendingAsync(
        Guid fileId,
        DateTime now,
        FileRetentionOptions options,
        CancellationToken ct);

    // =====================================================
    // PENDING PHYSICAL CLEANUP
    // =====================================================

    Task<List<StoredFile>> GetCleanupPendingFilesAsync(
        int batchSize,
        CancellationToken ct);

    // =====================================================
    // PERSISTENCE
    // =====================================================

    Task<int> SaveChangesAsync(
        CancellationToken ct);
}

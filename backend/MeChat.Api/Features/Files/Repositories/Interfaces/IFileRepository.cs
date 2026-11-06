
using MeChat.Api.Features.Files;

namespace MeChat.Api.Features.Files.Repositories.Interfaces;

public interface IFileRepository
{
    // =====================================================
    // FILE PERSISTENCE
    // =====================================================

    void AddStoredFile(StoredFile file);

    Task<int> SaveChangesAsync(CancellationToken ct);

    // =====================================================
    // FILE READING
    // =====================================================

    Task<StoredFile?> GetAvailableFileAsync(
        Guid fileId,
        CancellationToken ct);

    Task<List<Guid>> GetReferencedMessageIdsAsync(
        Guid fileId,
        CancellationToken ct);

    Task<bool> IsActiveUserAvatarAsync(
        Guid fileId,
        CancellationToken ct);

    Task<List<Guid>> GetGroupAvatarConversationIdsAsync(
        Guid fileId,
        CancellationToken ct);

    // =====================================================
    // USER AVATAR
    // =====================================================

    Task<bool> SetUserAvatarAtomicAsync(
        Guid actor,
        Guid fileId,
        CancellationToken ct);
}

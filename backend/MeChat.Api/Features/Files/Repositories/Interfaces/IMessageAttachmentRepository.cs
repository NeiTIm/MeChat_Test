
using MeChat.Api.Features.Files;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Files.Repositories.Interfaces;

public interface IMessageAttachmentRepository
{
    // =====================================================
    // PENDING FILES
    // =====================================================

    Task<List<StoredFile>> GetFilesByIdsAsync(
        IReadOnlyCollection<Guid> fileIds,
        CancellationToken ct);

    // =====================================================
    // ATTACH MESSAGE
    // =====================================================

    void AddMessageAttachment(
        MessageAttachment attachment);

    // =====================================================
    // ATTACHMENT READ
    // =====================================================

    Task<List<AttachmentDto>> GetMessageAttachmentsAsync(
        Guid messageId,
        CancellationToken ct);
}

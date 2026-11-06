
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Files;
using MeChat.Api.Features.Files.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Files.Repositories;

public sealed class MessageAttachmentRepository(
    AppDbContext db) : IMessageAttachmentRepository
{
    // =====================================================
    // GET STORED FILES BY IDS
    // =====================================================

    public Task<List<StoredFile>> GetFilesByIdsAsync(
        IReadOnlyCollection<Guid> fileIds,
        CancellationToken ct)
    {
        if (fileIds.Count == 0)
        {
            return Task.FromResult(
                new List<StoredFile>());
        }

        var ids = fileIds.ToArray();

        // Tracking is required because BindPendingAsync
        // changes StoredFile.Status to "Attached".
        //
        // Do not use AsNoTracking() here.

        return db.StoredFiles
            .Where(file => ids.Contains(file.Id))
            .ToListAsync(ct);
    }

    // =====================================================
    // ADD MESSAGE ATTACHMENT
    // =====================================================

    public void AddMessageAttachment(
        MessageAttachment attachment)
    {
        // Only attach the new entity to ChangeTracker.
        //
        // SaveChanges is the responsibility of the
        // outer message creation transaction.

        db.MessageAttachments.Add(attachment);
    }

    // =====================================================
    // GET MESSAGE ATTACHMENT DTOS
    // =====================================================

    public Task<List<AttachmentDto>> GetMessageAttachmentsAsync(
        Guid messageId,
        CancellationToken ct)
    {
        return db.MessageAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.MessageId == messageId)

            .Join(
                db.StoredFiles.AsNoTracking(),

                attachment => attachment.StoredFileId,

                file => file.Id,

                (attachment, file) => new
                {
                    attachment.DisplayOrder,
                    file.Id,
                    file.OriginalFileName,
                    file.ContentType,
                    file.SizeBytes
                })

            .OrderBy(item =>
                item.DisplayOrder)

            .Select(item => new AttachmentDto(
                item.Id,
                item.OriginalFileName,
                item.ContentType,
                item.SizeBytes))

            .ToListAsync(ct);
    }
}

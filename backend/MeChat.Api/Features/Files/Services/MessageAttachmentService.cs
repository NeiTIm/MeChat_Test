
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Files.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Files;

public sealed class MessageAttachmentService(
    IMessageAttachmentRepository repository)
{
    // =====================================================
    // BIND PENDING ATTACHMENTS TO MESSAGE
    // =====================================================

    public async Task<bool> BindPendingAsync(
        Guid actor,
        Guid messageId,
        IReadOnlyList<Guid>? attachmentIds,
        CancellationToken ct)
    {
        // =================================================
        // NORMALIZE ATTACHMENT IDS
        // =================================================

        var ids = attachmentIds?.ToArray() ?? [];

        // =================================================
        // VALIDATE MAXIMUM COUNT
        // =================================================

        // Maximum 5 attachments per message.

        if (ids.Length > 5)
        {
            return false;
        }

        // =================================================
        // VALIDATE UNIQUE IDS
        // =================================================

        if (ids.Distinct().Count() != ids.Length)
        {
            return false;
        }

        // =================================================
        // VALIDATE NON-EMPTY GUID
        // =================================================

        if (ids.Any(id => id == Guid.Empty))
        {
            return false;
        }

        // =================================================
        // NO ATTACHMENTS
        // =================================================

        if (ids.Length == 0)
        {
            return true;
        }

        // =================================================
        // LOAD STORED FILES
        // =================================================

        var files =
            await repository.GetFilesByIdsAsync(
                ids,
                ct);

        // Every ID must correspond to an existing file.
        if (files.Count != ids.Length)
        {
            return false;
        }

        // =================================================
        // VALIDATE FILE OWNERSHIP AND STATUS
        // =================================================

        foreach (var file in files)
        {
            // Only the original uploader may attach.
            if (file.UploadedByUserId != actor)
            {
                return false;
            }

            // Only files uploaded for messages.
            if (file.Purpose != "Message")
            {
                return false;
            }

            // File must not already be attached.
            if (file.Status != "Pending")
            {
                return false;
            }

            // Deleted files cannot be attached.
            if (file.DeletedAt is not null)
            {
                return false;
            }
        }

        // =================================================
        // BIND FILES IN CLIENT-SPECIFIED ORDER
        // =================================================

        var filesById = files.ToDictionary(
            file => file.Id);

        for (var index = 0; index < ids.Length; index++)
        {
            var file = filesById[ids[index]];

            // =================================================
            // CHANGE FILE STATUS
            // =================================================

            file.Status = "Attached";

            // =================================================
            // CREATE MESSAGE ATTACHMENT
            // =================================================

            repository.AddMessageAttachment(
                new MessageAttachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    StoredFileId = file.Id,
                    DisplayOrder = index
                });
        }

        // =================================================
        // DO NOT SAVE HERE
        // =================================================

        // The outer message creation transaction must
        // commit these changes together with the Message.
        //
        // Preserve the original service behavior.

        return true;
    }

    // =====================================================
    // ENRICH A SINGLE MESSAGE
    // =====================================================

    public async Task<MessageDto> EnrichAsync(
        MessageDto message,
        CancellationToken ct)
    {
        // =================================================
        // LOAD ATTACHMENTS
        // =================================================

        var attachments =
            await repository.GetMessageAttachmentsAsync(
                message.Id,
                ct);

        // =================================================
        // HANDLE RECALLED MESSAGE
        // =================================================

        // A recalled message must not expose attachment
        // metadata in its API response.

        if (message.Recalled)
        {
            return message with
            {
                Attachments = []
            };
        }

        // =================================================
        // RETURN ENRICHED MESSAGE
        // =================================================

        return message with
        {
            Attachments = attachments
        };
    }

    // =====================================================
    // ENRICH MESSAGE PAGE
    // =====================================================

    public async Task<MessagePageDto> EnrichAsync(
        MessagePageDto page,
        CancellationToken ct)
    {
        // =================================================
        // PREPARE RESULT
        // =================================================

        var items = new List<MessageDto>(
            page.Items.Count);

        // =================================================
        // ENRICH EACH MESSAGE
        // =================================================

        foreach (var message in page.Items)
        {
            var enrichedMessage =
                await EnrichAsync(
                    message,
                    ct);

            items.Add(enrichedMessage);
        }

        // =================================================
        // RETURN PAGE
        // =================================================

        return page with
        {
            Items = items
        };
    }
}

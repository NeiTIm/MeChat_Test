
using System.Globalization;
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Files;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Messages.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Realtime.Interfaces;

namespace MeChat.Api.Features.Messages;

public sealed class MessageActionService(
    IMessageActionRepository repository,
    TimeProvider clock,
    PrivateChatService privateChats,
    GroupService groups,
    IRealtimePublisher publisher,
    ILogger<MessageActionService> logger,
    MessageAttachmentService attachments)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // GET MESSAGE
    // =====================================================

    public Task<MessageDto?> GetAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        return AtomicAsync<MessageDto?>(
            async _ =>
            {
                var message =
                    await repository.GetMessageAsync(
                        messageId,
                        ct);

                if (message is null ||
                    !await CanViewAsync(actor, message, ct))
                {
                    return null;
                }

                return await attachments.EnrichAsync(
                    Map(message),
                    ct);
            },
            ct);
    }

    // =====================================================
    // GET MESSAGE CONTEXT
    // =====================================================

    public Task<MessageContextDto?> ContextAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        return AtomicAsync<MessageContextDto?>(
            async _ =>
            {
                var message =
                    await repository.GetMessageAsync(
                        messageId,
                        ct);

                if (message is null ||
                    !await CanViewAsync(actor, message, ct))
                {
                    return null;
                }

                // Recalled messages have no visible context.
                if (message.RecalledAt is not null)
                {
                    return new MessageContextDto(
                        null,
                        [],
                        null);
                }

                // =================================================
                // REPLY PREVIEW
                // =================================================

                ReplyPreviewDto? reply = null;

                if (message.ReplyToMessageId is { } sourceId)
                {
                    var source =
                        await repository.GetMessageAsync(
                            sourceId,
                            ct);

                    var available =
                        source is not null &&
                        source.ConversationId ==
                            message.ConversationId &&
                        source.RecalledAt is null &&
                        await CanViewAsync(actor, source, ct);

                    string? content = null;

                    if (available)
                    {
                        if (source!.Content is { } text)
                        {
                            content = text[
                                ..Math.Min(text.Length, 200)];
                        }
                        else
                        {
                            content = "Attachment";
                        }
                    }

                    reply = new ReplyPreviewDto(
                        available,
                        content);
                }

                // =================================================
                // REACTIONS
                // =================================================

                var reactions =
                    await repository.GetReactionCountsAsync(
                        messageId,
                        ct);

                var mine =
                    await repository.GetMyReactionAsync(
                        messageId,
                        actor,
                        ct);

                return new MessageContextDto(
                    reply,
                    reactions,
                    mine);
            },
            ct);
    }

    // =====================================================
    // EDIT MESSAGE
    // =====================================================

    public async Task<MessageDto?> EditAsync(
        Guid actor,
        Guid messageId,
        EditMessageRequest input,
        CancellationToken ct)
    {
        var content = input.Content?.Trim();

        if (string.IsNullOrWhiteSpace(content) ||
            content.Length > 10000)
        {
            return null;
        }

        var message = await AtomicAsync<Message?>(
            async afterCommit =>
            {
                var current =
                    await repository.GetMessageAsync(
                        messageId,
                        ct);

                if (current is null ||
                    current.SenderUserId != actor ||
                    current.Content is null ||
                    current.RecalledAt is not null ||
                    current.CreatedAt > Now ||
                    Now - current.CreatedAt >
                        TimeSpan.FromMinutes(15) ||
                    !await CanViewAsync(actor, current, ct) ||
                    !await CanMutateAsync(
                        actor,
                        current.ConversationId,
                        ct))
                {
                    return null;
                }

                // No edit history/event for unchanged content.
                if (current.Content == content)
                    return current;

                // =================================================
                // CREATE EDIT VERSION
                // =================================================

                var version =
                    await repository.GetEditVersionCountAsync(
                        messageId,
                        ct) + 1;

                var now = Now;

                repository.AddEditHistory(
                    new MessageEditHistory
                    {
                        Id = Guid.NewGuid(),
                        MessageId = messageId,
                        Version = version,
                        PreviousContent = current.Content,
                        EditedAt = now
                    });

                // =================================================
                // UPDATE MESSAGE
                // =================================================

                current.Content = content;
                current.EditedAt = now;

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME AFTER COMMIT
                // =================================================

                afterCommit.Add(() =>
                    PublishAsync(
                        current,
                        "MessageEdited",
                        ct));

                return current;
            },
            ct,
            enableRetry: true);

        if (message is null)
            return null;

        return await attachments.EnrichAsync(
            Map(message),
            ct);
    }

    // =====================================================
    // MESSAGE EDIT HISTORY
    // =====================================================

    public async Task<List<EditVersionDto>?> EditHistoryAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        var message =
            await repository.GetMessageAsync(
                messageId,
                ct);

        if (message is null ||
            message.RecalledAt is not null ||
            !await CanViewAsync(actor, message, ct))
        {
            return null;
        }

        var versions =
            await repository.GetEditVersionsAsync(
                messageId,
                ct);

        // =================================================
        // RECHECK AUTHORIZATION / RECALL STATE
        // =================================================

        if (!await CanViewAsync(actor, message, ct) ||
            await repository.IsMessageRecalledAsync(
                messageId, ct))
        {
            return null;
        }

        if (message.EditedAt is not null)
        {
            versions.Add(
                new EditVersionDto(
                    versions.Count + 1,
                    message.Content ?? "",
                    message.EditedAt.Value));
        }

        return versions;
    }

    // =====================================================
    // RECALL MESSAGE
    // =====================================================

    public Task<bool> RecallAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var message =
                    await repository.GetMessageAsync(
                        messageId,
                        ct);

                if (message is null ||
                    message.SenderUserId != actor ||
                    message.RecalledAt is not null ||
                    !await CanViewAsync(actor, message, ct))
                {
                    return false;
                }

                var conversation =
                    await repository.GetConversationAsync(
                        message.ConversationId,
                        ct);

                if (conversation is null)
                    return false;

                // =================================================
                // RECALL AUTHORIZATION
                // =================================================

                if (conversation.Type == "Group")
                {
                    if (conversation.Status != "Active")
                        return false;
                }
                else
                {
                    if (!await privateChats.CanSendMessageAsync(
                            actor,
                            conversation.Id,
                            ct))
                    {
                        return false;
                    }
                }

                // =================================================
                // RECALL
                // =================================================

                message.Content = null;
                message.RecalledAt = Now;

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    PublishAsync(
                        message,
                        "MessageRecalled",
                        ct));

                return true;
            },
            ct);
    }

    // =====================================================
    // DELETE MESSAGE FOR ME
    // =====================================================

    public Task<bool> DeleteForMeAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var message =
                    await repository.GetMessageAsync(
                        messageId,
                        ct);

                if (message is null ||
                    !await CanViewAsync(actor, message, ct))
                {
                    return false;
                }

                // =================================================
                // GROUP STATUS
                // =================================================

                var inactiveGroup =
                    await repository.IsInactiveGroupAsync(
                        message.ConversationId,
                        ct);

                if (inactiveGroup)
                    return false;

                // =================================================
                // DELETE-FOR-ME STATE
                // =================================================

                var exists =
                    await repository.HasMessageUserStateAsync(
                        messageId,
                        actor,
                        ct);

                if (!exists)
                {
                    repository.AddMessageUserState(
                        new MessageUserState
                        {
                            MessageId = messageId,
                            UserId = actor,
                            DeletedAt = Now
                        });

                    await repository.SaveChangesAsync(ct);
                }

                // =================================================
                // REALTIME AFTER COMMIT
                // =================================================

                afterCommit.Add(async () =>
                {
                    try
                    {
                        await publisher.SendToUserAsync(
                            actor.ToString(),
                            "MessageDeletedForMe",
                            new
                            {
                                messageId
                            },
                            ct);
                    }
                    catch (Exception error)
                    {
                        logger.LogWarning(
                            error,
                            "Message delete event delivery failed for {MessageId}",
                            messageId);
                    }
                });

                return true;
            },
            ct);
    }

    // =====================================================
    // SET MESSAGE REACTION
    // =====================================================

    public Task<bool> SetReactionAsync(
        Guid actor,
        Guid messageId,
        string emoji,
        CancellationToken ct)
    {
        emoji = emoji?.Trim() ?? "";

        if (emoji.Length is < 1 or > 16 ||
            StringInfo.ParseCombiningCharacters(emoji).Length != 1)
        {
            return Task.FromResult(false);
        }

        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var message =
                    await repository.GetMessageAsync(
                        messageId,
                        ct);

                if (message is null ||
                    message.RecalledAt is not null ||
                    !await CanViewAsync(actor, message, ct) ||
                    !await CanMutateAsync(
                        actor,
                        message.ConversationId,
                        ct))
                {
                    return false;
                }

                // =================================================
                // GET EXISTING REACTION
                // =================================================

                var reaction =
                    await repository.GetReactionAsync(
                        messageId,
                        actor,
                        ct);

                var now = Now;

                if (reaction is null)
                {
                    repository.AddReaction(
                        new MessageReaction
                        {
                            Id = Guid.NewGuid(),
                            MessageId = messageId,
                            UserId = actor,
                            Emoji = emoji,
                            CreatedAt = now
                        });
                }
                else
                {
                    reaction.Emoji = emoji;
                    reaction.CreatedAt = now;
                }

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    PublishAsync(
                        message,
                        "MessageReactionChanged",
                        ct));

                return true;
            },
            ct,
            enableRetry: true);
    }

    // =====================================================
    // REMOVE MESSAGE REACTION
    // =====================================================

    public Task<bool> RemoveReactionAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var message =
                    await repository.GetMessageAsync(
                        messageId,
                        ct);

                if (message is null ||
                    !await CanViewAsync(actor, message, ct) ||
                    !await CanMutateAsync(
                        actor,
                        message.ConversationId,
                        ct))
                {
                    return false;
                }

                var reaction =
                    await repository.GetReactionAsync(
                        messageId,
                        actor,
                        ct);

                if (reaction is null)
                    return false;

                repository.RemoveReaction(reaction);

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    PublishAsync(
                        message,
                        "MessageReactionChanged",
                        ct));

                return true;
            },
            ct);
    }

    // =====================================================
    // FORWARD MESSAGE
    // =====================================================

    public async Task<MessageDto?> ForwardAsync(
        Guid actor,
        Guid sourceId,
        ForwardMessageRequest input,
        CancellationToken ct)
    {
        if (input.DestinationConversationId == Guid.Empty ||
            input.ClientMessageId == Guid.Empty)
        {
            return null;
        }

        var message = await AtomicAsync<Message?>(
            async afterCommit =>
            {
                // =================================================
                // IDEMPOTENCY
                // =================================================

                var previous =
                    await repository.GetMessageByClientIdAsync(
                        actor,
                        input.ClientMessageId,
                        ct);

                if (previous is not null)
                {
                    var valid =
                        previous.ConversationId ==
                            input.DestinationConversationId &&
                        previous.IsForwarded &&
                        await CanViewAsync(
                            actor, previous, ct) &&
                        await CanMutateAsync(
                            actor,
                            previous.ConversationId,
                            ct);

                    // Duplicate forward does not create another
                    // message or publish new notifications.
                    return valid ? previous : null;
                }

                // =================================================
                // SOURCE MESSAGE
                // =================================================

                var source =
                    await repository.GetUnrecalledMessageAsync(
                        sourceId,
                        ct);

                if (source is null ||
                    !await CanViewAsync(actor, source, ct))
                {
                    return null;
                }

                // =================================================
                // DESTINATION
                // =================================================

                var destination =
                    await repository.GetConversationAsync(
                        input.DestinationConversationId,
                        ct);

                if (destination is null ||
                    destination.Status != "Active")
                {
                    return null;
                }

                bool allowed;

                if (destination.Type == "Group")
                {
                    allowed = await groups.CanSendAsync(
                        actor, destination.Id, ct);
                }
                else
                {
                    // PrepareSendAsync may modify the shared
                    // scoped DbContext. It must remain inside
                    // this database transaction.
                    allowed = await privateChats.PrepareSendAsync(
                        actor, destination.Id, ct);
                }

                if (!allowed)
                    return null;

                // Recheck that the source was not recalled.
                if (!await repository.IsMessageUnrecalledAsync(
                        sourceId, ct))
                {
                    return null;
                }

                // =================================================
                // ADVANCE CONVERSATION SEQUENCE
                // =================================================

                var now = Now;

                destination.LastSequence++;
                destination.UpdatedAt = now;

                // =================================================
                // CREATE FORWARDED MESSAGE
                // =================================================

                var forwarded = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = destination.Id,
                    SenderUserId = actor,
                    ClientMessageId = input.ClientMessageId,
                    Sequence = destination.LastSequence,
                    Type = source.Type,
                    Content = source.Content,
                    IsForwarded = true,
                    Status = "Sent",
                    CreatedAt = now
                };

                repository.AddMessage(forwarded);

                // =================================================
                // COPY ATTACHMENT REFERENCES
                // =================================================

                var sourceAttachments =
                    await repository.GetMessageAttachmentsAsync(
                        sourceId,
                        ct);

                foreach (var attachment in sourceAttachments)
                {
                    repository.AddMessageAttachment(
                        new MessageAttachment
                        {
                            Id = Guid.NewGuid(),
                            MessageId = forwarded.Id,
                            StoredFileId = attachment.StoredFileId,
                            DisplayOrder = attachment.DisplayOrder
                        });
                }

                // =================================================
                // RECIPIENTS
                // =================================================

                List<Guid> recipients;

                if (destination.Type == "Group")
                {
                    recipients =
                        await repository.GetGroupRecipientsAsync(
                            destination.Id,
                            actor,
                            ct);
                }
                else
                {
                    var pair =
                        await repository.GetPrivateRecipientsAsync(
                            destination.Id,
                            ct);

                    recipients = pair
                        .Select(userId =>
                            userId == actor
                                ? Guid.Empty
                                : userId)
                        .Where(userId =>
                            userId != Guid.Empty)
                        .ToList();
                }

                // =================================================
                // NOTIFICATIONS
                // =================================================

                var notifications =
                    new List<Notification>();

                foreach (var recipient in recipients.Distinct())
                {
                    var muted =
                        await repository.IsConversationMutedAsync(
                            destination.Id,
                            recipient,
                            Now,
                            ct);

                    if (muted)
                        continue;

                    string notificationType;

                    if (destination.Type == "Group")
                    {
                        notificationType = "GroupMessage";
                    }
                    else
                    {
                        var accepted =
                            await privateChats.CanSendAcceptedAsync(
                                actor,
                                destination.Id,
                                ct);

                        notificationType = accepted
                            ? "Message"
                            : "MessageRequest";
                    }

                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        RecipientUserId = recipient,
                        ActorUserId = actor,
                        Type = notificationType,
                        ReferenceId = forwarded.Id,
                        CreatedAt = Now
                    };

                    repository.AddNotification(notification);
                    notifications.Add(notification);
                }

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME AFTER COMMIT
                // =================================================

                afterCommit.Add(async () =>
                {
                    await PublishAsync(
                        forwarded,
                        "MessageCreated",
                        ct);

                    foreach (var notification in notifications)
                    {
                        if (!await CanViewAsync(
                                notification.RecipientUserId,
                                forwarded,
                                ct))
                        {
                            continue;
                        }

                        try
                        {
                            await publisher.SendToUserAsync(
                                notification.RecipientUserId.ToString(),
                                "NotificationCreated",
                                new
                                {
                                    notificationId = notification.Id,
                                    type = notification.Type,
                                    referenceId = forwarded.Id
                                },
                                ct);
                        }
                        catch (Exception error)
                        {
                            logger.LogWarning(
                                error,
                                "Forward notification delivery failed");
                        }
                    }
                });

                return forwarded;
            },
            ct,
            enableRetry: true);

        if (message is null)
            return null;

        return await attachments.EnrichAsync(
            Map(message),
            ct);
    }

    // =====================================================
    // SEARCH MESSAGES
    // =====================================================

    public async Task<MessagePageDto?> SearchAsync(
        Guid actor,
        Guid conversationId,
        string query,
        long? beforeSequence,
        int limit,
        CancellationToken ct)
    {
        query = query?.Trim() ?? "";

        if (query.Length is < 2 or > 100)
            return null;

        if (!await CanReadConversationAsync(
                actor, conversationId, ct))
        {
            return null;
        }

        var take = Math.Clamp(limit, 1, 50);

        var found = new List<MessageDto>(take + 1);

        long? cursor = beforeSequence;

        // =====================================================
        // BATCHED SEARCH
        // =====================================================

        while (found.Count <= take)
        {
            var batch =
                await repository.SearchMessagesBatchAsync(
                    conversationId,
                    query,
                    cursor,
                    100,
                    ct);

            if (batch.Count == 0)
                break;

            foreach (var message in batch)
            {
                if (await CanViewAsync(
                        actor, message, ct))
                {
                    found.Add(Map(message));
                }

                if (found.Count > take)
                    break;
            }

            cursor = batch[^1].Sequence;

            if (batch.Count < 100 ||
                found.Count > take)
            {
                break;
            }
        }

        // =====================================================
        // RECHECK CONVERSATION PERMISSION
        // =====================================================

        if (!await CanReadConversationAsync(
                actor, conversationId, ct))
        {
            return null;
        }

        var hasMore = found.Count > take;

        var nextCursor = hasMore
            ? found[take - 1].Sequence
            : (long?)null;

        var page = new MessagePageDto(
            found.Take(take).ToList(),
            nextCursor,
            hasMore);

        return await attachments.EnrichAsync(
            page,
            ct);
    }

    // =====================================================
    // CAN READ CONVERSATION
    // =====================================================

    private async Task<bool> CanReadConversationAsync(
        Guid actor,
        Guid conversationId,
        CancellationToken ct)
    {
        var type =
            await repository.GetConversationTypeAsync(
                conversationId,
                ct);

        if (type == "Group")
        {
            return await groups.CanReadAsync(
                actor,
                conversationId,
                ct);
        }

        if (type != "Private")
            return false;

        return await privateChats.CanReadAsync(
                   actor, conversationId, ct)
               ||
               await privateChats.CanReadRequestAsync(
                   actor, conversationId, ct);
    }

    // =====================================================
    // CAN MUTATE MESSAGE
    // =====================================================

    private async Task<bool> CanMutateAsync(
        Guid actor,
        Guid conversationId,
        CancellationToken ct)
    {
        var type =
            await repository.GetConversationTypeAsync(
                conversationId,
                ct);

        if (type == "Group")
        {
            return await groups.CanSendAsync(
                actor,
                conversationId,
                ct);
        }

        if (type != "Private")
            return false;

        return await privateChats.CanSendMessageAsync(
            actor,
            conversationId,
            ct);
    }

    // =====================================================
    // CAN VIEW MESSAGE
    // =====================================================

    private async Task<bool> CanViewAsync(
        Guid actor,
        Message message,
        CancellationToken ct)
    {
        // =================================================
        // DELETE FOR ME
        // =================================================

        var deleted =
            await repository.HasMessageUserStateAsync(
                message.Id,
                actor,
                ct);

        if (deleted)
            return false;

        // =================================================
        // CONVERSATION TYPE
        // =================================================

        var type =
            await repository.GetConversationTypeAsync(
                message.ConversationId,
                ct);

        if (type == "Group")
        {
            return await groups.CanViewMessageAsync(
                actor,
                message.ConversationId,
                message.Sequence,
                ct);
        }

        if (type != "Private")
            return false;

        // =================================================
        // HIDDEN CURSOR
        // =================================================

        var hidden =
            await repository.GetHiddenThroughSequenceAsync(
                message.ConversationId,
                actor,
                ct);

        if (message.Sequence <= hidden)
            return false;

        // =================================================
        // PRIVATE CHAT PERMISSION
        // =================================================

        return await privateChats.CanReadAsync(
                   actor,
                   message.ConversationId,
                   ct)
               ||
               await privateChats.CanReadRequestAsync(
                   actor,
                   message.ConversationId,
                   ct);
    }

    // =====================================================
    // CAN VIEW ATTACHMENT
    // =====================================================

    public async Task<bool> CanViewAttachmentAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        var message =
            await repository.GetUnrecalledMessageAsync(
                messageId,
                ct);

        return message is not null &&
               await CanViewAsync(
                   actor,
                   message,
                   ct);
    }

    // =====================================================
    // PUBLISH MESSAGE REALTIME
    // =====================================================

    private async Task PublishAsync(
        Message message,
        string eventName,
        CancellationToken ct)
    {
        var type =
            await repository.GetConversationTypeAsync(
                message.ConversationId,
                ct);

        List<Guid> recipients;

        if (type == "Group")
        {
            recipients =
                await repository.GetGroupRecipientsAsync(
                    message.ConversationId,
                    null,
                    ct);
        }
        else if (type == "Private")
        {
            recipients =
                await repository.GetPrivateRecipientsAsync(
                    message.ConversationId,
                    ct);
        }
        else
        {
            return;
        }

        foreach (var recipient in recipients)
        {
            // =================================================
            // REALTIME RECIPIENT AUTHORIZATION
            // =================================================

            bool allowed;

            if (type == "Group")
            {
                allowed = await groups.CanViewMessageAsync(
                    recipient,
                    message.ConversationId,
                    message.Sequence,
                    ct);
            }
            else
            {
                allowed =
                    await privateChats.CanReadAsync(
                        recipient,
                        message.ConversationId,
                        ct)
                    ||
                    await privateChats.CanReadRequestAsync(
                        recipient,
                        message.ConversationId,
                        ct);
            }

            if (!allowed)
                continue;

            // =================================================
            // DELIVER EVENT
            // =================================================

            try
            {
                await publisher.SendToUserAsync(
                    recipient.ToString(),
                    eventName,
                    new
                    {
                        eventId = Guid.NewGuid(),
                        conversationId = message.ConversationId,
                        messageId = message.Id,
                        sequence = message.Sequence
                    },
                    ct);
            }
            catch (Exception error)
            {
                logger.LogWarning(
                    error,
                    "Message action event delivery failed for {MessageId}",
                    message.Id);
            }
        }
    }

    // =====================================================
    // MAP MESSAGE ENTITY TO DTO
    // =====================================================

    private static MessageDto Map(Message message)
    {
        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.ClientMessageId,
            message.Sequence,
            message.RecalledAt is null
                ? message.Content
                : null,
            message.CreatedAt,
            message.RecalledAt is not null,
            message.EditedAt,
            message.IsForwarded,
            message.ReplyToMessageId);
    }

    // =====================================================
    // TRANSACTION + AFTER COMMIT
    // =====================================================

    private async Task<T> AtomicAsync<T>(
        Func<List<Func<Task>>, Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false)
    {
        // Local to one operation, never shared
        // between concurrent HTTP requests.
        var afterCommit = new List<Func<Task>>();

        var result =
            await repository.ExecuteAtomicAsync(
                async () =>
                {
                    // Discard realtime events registered by
                    // earlier failed transaction attempts.
                    afterCommit.Clear();

                    return await operation(afterCommit);
                },
                result =>
                    result is not null &&
                    (result is not bool success || success),
                ct,
                enableRetry);

        // No side effects for a failed business operation.
        if (result is null ||
            result is bool succeeded && !succeeded)
        {
            return result;
        }

        // All database changes have been committed.
        foreach (var action in afterCommit)
        {
            await action();
        }

        return result;
    }
}

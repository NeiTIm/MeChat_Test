
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Messages.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Realtime.Interfaces;

namespace MeChat.Api.Features.Messages;

public sealed class MessageStatusService(
    IMessageStatusRepository repository,
    TimeProvider clock,
    PrivateChatService privateChats,
    GroupService groups,
    IRealtimePublisher publisher,
    ILogger<MessageStatusService> logger)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // GET MESSAGE RECEIPT
    // =====================================================

    public async Task<MessageReceiptDto?> ReceiptAsync(
        Guid actor,
        Guid messageId,
        CancellationToken ct)
    {
        // =================================================
        // MESSAGE OWNERSHIP
        // =================================================

        var message =
            await repository.GetSentMessageAsync(
                actor,
                messageId,
                ct);

        if (message is null)
            return null;

        if (!await CanViewMessageAsync(
                actor,
                message,
                ct))
        {
            return null;
        }

        // =================================================
        // DELIVERED RECIPIENTS
        // =================================================

        var recipients =
            await repository.GetDeliveredRecipientIdsAsync(
                messageId,
                ct);

        var delivered = 0;
        var seen = 0;

        foreach (var recipient in recipients)
        {
            // Do not expose receipt information when
            // the recipient can no longer view the message.
            if (!await CanViewMessageAsync(
                    recipient,
                    message,
                    ct))
            {
                continue;
            }

            delivered++;

            // =================================================
            // SEEN PRIVACY
            // =================================================

            // A user in Message Requests may acknowledge
            // delivery, but their Seen state must not
            // be exposed to a stranger.

            var canReadMainChat =
                await CanReadMainChatAsync(
                    recipient,
                    message.ConversationId,
                    ct);

            if (!canReadMainChat)
                continue;

            var hasRead =
                await repository.HasReadThroughAsync(
                    message.ConversationId,
                    recipient,
                    message.Sequence,
                    ct);

            if (hasRead)
                seen++;
        }

        // Recheck authorization before returning.
        if (!await CanViewMessageAsync(
                actor,
                message,
                ct))
        {
            return null;
        }

        // =================================================
        // AGGREGATE MESSAGE STATUS
        // =================================================

        var status =
            seen > 0
                ? "Seen"
                : delivered > 0
                    ? "Delivered"
                    : "Sent";

        return new MessageReceiptDto(
            status,
            delivered,
            seen);
    }

    // =====================================================
    // ACKNOWLEDGE MESSAGE DELIVERY
    // =====================================================

    public Task<bool> AcknowledgeAsync(
        Guid actor,
        Guid conversationId,
        DeliveryAcksRequest input,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE MESSAGE IDS
        // =================================================

        var ids = input.MessageIds?
            .Distinct()
            .ToArray() ?? [];

        if (ids.Length is < 1 or > 100 ||
            ids.Any(id => id == Guid.Empty))
        {
            return Task.FromResult(false);
        }

        // =================================================
        // ATOMIC DELIVERY ACK
        // =================================================

        return AtomicAsync<bool>(
            async afterCommit =>
            {
                // =================================================
                // INACTIVE GROUP CHECK
                // =================================================

                var inactiveGroup =
                    await repository.IsInactiveGroupAsync(
                        conversationId,
                        ct);

                if (inactiveGroup)
                    return false;

                // =================================================
                // GET MESSAGES
                // =================================================

                var messages =
                    await repository.GetMessagesByIdsAsync(
                        conversationId,
                        ids,
                        ct);

                // All requested messages must exist
                // in the specified conversation.
                if (messages.Count != ids.Length)
                    return false;

                // A sender cannot acknowledge delivery
                // of their own messages.
                if (messages.Any(message =>
                        message.SenderUserId == actor))
                {
                    return false;
                }

                // =================================================
                // AUTHORIZATION
                // =================================================

                foreach (var message in messages)
                {
                    if (!await CanViewMessageAsync(
                            actor,
                            message,
                            ct))
                    {
                        return false;
                    }
                }

                // =================================================
                // ALREADY ACKNOWLEDGED
                // =================================================

                var existingIds =
                    await repository
                        .GetExistingDeliveryMessageIdsAsync(
                            actor,
                            ids,
                            ct);

                var existingSet =
                    existingIds.ToHashSet();

                var added = messages
                    .Where(message =>
                        !existingSet.Contains(message.Id))
                    .ToArray();

                // =================================================
                // CREATE DELIVERY RECORDS
                // =================================================

                // Capture the timestamp once so that the
                // database value and realtime payload match.
                var now = Now;

                foreach (var message in added)
                {
                    repository.AddDelivery(
                        new MessageDelivery
                        {
                            MessageId = message.Id,
                            RecipientUserId = actor,
                            DeliveredAt = now
                        });
                }

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME AFTER COMMIT
                // =================================================

                foreach (var message in added)
                {
                    var senderUserId = message.SenderUserId;
                    var deliveredMessageId = message.Id;

                    afterCommit.Add(async () =>
                    {
                        var payload = new
                        {
                            eventId = Guid.NewGuid(),
                            conversationId,
                            messageId = deliveredMessageId,
                            recipientUserId = actor,
                            deliveredAt = now
                        };

                        // Notify original sender.
                        await SafeEventAsync(
                            senderUserId,
                            "MessageDelivered",
                            payload,
                            ct);

                        // Synchronize receiver's devices.
                        await SafeEventAsync(
                            actor,
                            "MessageDelivered",
                            payload,
                            ct);
                    });
                }

                return true;
            },
            ct,
            enableRetry: true);
    }

    // =====================================================
    // MARK CONVERSATION AS READ
    // =====================================================

    public Task<bool> MarkReadAsync(
        Guid actor,
        Guid conversationId,
        long sequence,
        CancellationToken ct)
    {
        if (sequence < 0)
            return Task.FromResult(false);

        return AtomicAsync<bool>(
            async afterCommit =>
            {
                // =================================================
                // MAIN CHAT PERMISSION
                // =================================================

                if (!await CanReadMainChatAsync(
                        actor,
                        conversationId,
                        ct))
                {
                    return false;
                }

                // =================================================
                // VALIDATE MAX SEQUENCE
                // =================================================

                var maxSequence =
                    await repository.GetLastSequenceAsync(
                        conversationId,
                        ct);

                if (maxSequence is null ||
                    sequence > maxSequence.Value)
                {
                    return false;
                }

                // =================================================
                // VERIFY TARGET MESSAGE VISIBILITY
                // =================================================

                if (sequence > 0)
                {
                    var viewed =
                        await repository.GetMessageAtSequenceAsync(
                            conversationId,
                            sequence,
                            ct);

                    if (viewed is null ||
                        !await CanViewMessageAsync(
                            actor,
                            viewed,
                            ct))
                    {
                        return false;
                    }
                }

                // =================================================
                // GET OR CREATE CURSOR
                // =================================================

                var cursor =
                    await repository.GetReadCursorAsync(
                        conversationId,
                        actor,
                        ct);

                if (cursor is null)
                {
                    cursor = new ConversationReadCursor
                    {
                        ConversationId = conversationId,
                        UserId = actor,
                        LastReadSequence = 0,
                        UpdatedAt = Now
                    };

                    repository.AddReadCursor(cursor);
                }

                // =================================================
                // MONOTONIC READ CURSOR
                // =================================================

                var changed =
                    sequence > cursor.LastReadSequence;

                if (changed)
                {
                    cursor.LastReadSequence = sequence;
                    cursor.UpdatedAt = Now;
                }

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME ONLY IF CURSOR ADVANCES
                // =================================================

                if (changed)
                {
                    afterCommit.Add(() =>
                        PublishSeenAsync(
                            actor,
                            conversationId,
                            sequence,
                            ct));
                }

                return true;
            },
            ct,
            enableRetry: true);
    }

    // =====================================================
    // GET UNREAD COUNT
    // =====================================================

    public async Task<UnreadDto?> UnreadAsync(
        Guid actor,
        Guid conversationId,
        CancellationToken ct)
    {
        // =================================================
        // INITIAL AUTHORIZATION
        // =================================================

        if (!await CanReadMainChatAsync(
                actor,
                conversationId,
                ct))
        {
            return null;
        }

        // =================================================
        // READ CURSOR
        // =================================================

        var cursor =
            await repository.GetReadSequenceAsync(
                conversationId,
                actor,
                ct);

        // =================================================
        // HIDDEN CURSOR
        // =================================================

        var hidden =
            await repository.GetHiddenThroughSequenceAsync(
                conversationId,
                actor,
                ct);

        // Ignore messages at or below either cursor.
        var lower = Math.Max(cursor, hidden);

        // =================================================
        // CONVERSATION TYPE
        // =================================================

        var type =
            await repository.GetConversationTypeAsync(
                conversationId,
                ct);

        if (type is not ("Private" or "Group"))
            return null;

        // =================================================
        // COUNT UNREAD
        // =================================================

        var count =
            await repository.CountUnreadMessagesAsync(
                actor,
                conversationId,
                lower,
                isPrivate: type == "Private",
                ct);

        // =================================================
        // RECHECK AUTHORIZATION
        // =================================================

        if (!await CanReadMainChatAsync(
                actor,
                conversationId,
                ct))
        {
            return null;
        }

        return new UnreadDto(
            count,
            cursor);
    }

    // =====================================================
    // CAN READ MAIN CHAT
    // =====================================================

    private async Task<bool> CanReadMainChatAsync(
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

        if (type == "Private")
        {
            return await privateChats.CanReadAsync(
                actor,
                conversationId,
                ct);
        }

        return false;
    }

    // =====================================================
    // CAN VIEW MESSAGE
    // =====================================================

    private async Task<bool> CanViewMessageAsync(
        Guid actor,
        Message message,
        CancellationToken ct)
    {
        // =================================================
        // DELETE FOR ME
        // =================================================

        var deleted =
            await repository.IsMessageDeletedForUserAsync(
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

        // =================================================
        // GROUP MESSAGE VISIBILITY
        // =================================================

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
        // PRIVATE CHAT HIDDEN CURSOR
        // =================================================

        var hidden =
            await repository.GetHiddenThroughSequenceAsync(
                message.ConversationId,
                actor,
                ct);

        if (message.Sequence <= hidden)
            return false;

        // =================================================
        // PRIVATE CHAT / MESSAGE REQUEST ACCESS
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
    // PUBLISH MESSAGE SEEN
    // =====================================================

    private async Task PublishSeenAsync(
        Guid actor,
        Guid conversationId,
        long sequence,
        CancellationToken ct)
    {
        var payload = new
        {
            eventId = Guid.NewGuid(),
            conversationId,
            userId = actor,
            lastReadSequence = sequence
        };

        // =================================================
        // SYNC ACTOR'S DEVICES
        // =================================================

        await SafeEventAsync(
            actor,
            "MessageSeen",
            payload,
            ct);

        var type =
            await repository.GetConversationTypeAsync(
                conversationId,
                ct);

        // =================================================
        // PRIVATE CHAT
        // =================================================

        if (type == "Private")
        {
            var other =
                await repository.GetOtherPrivateParticipantAsync(
                    conversationId,
                    actor,
                    ct);

            if (other is null)
                return;

            // Only main-chat participants receive Seen.
            // Message Request senders must not receive it.
            if (await privateChats.CanReadAsync(
                    other.Value,
                    conversationId,
                    ct))
            {
                await SafeEventAsync(
                    other.Value,
                    "MessageSeen",
                    payload,
                    ct);
            }

            return;
        }

        // =================================================
        // GROUP CHAT
        // =================================================

        if (type == "Group")
        {
            var memberIds =
                await repository.GetOtherActiveGroupMembersAsync(
                    conversationId,
                    actor,
                    ct);

            foreach (var memberId in memberIds)
            {
                if (!await groups.CanSendAsync(
                        memberId,
                        conversationId,
                        ct))
                {
                    continue;
                }

                await SafeEventAsync(
                    memberId,
                    "MessageSeen",
                    payload,
                    ct);
            }
        }
    }

    // =====================================================
    // SAFE REALTIME EVENT
    // =====================================================

    private async Task SafeEventAsync(
        Guid recipient,
        string name,
        object payload,
        CancellationToken ct)
    {
        try
        {
            await publisher.SendToUserAsync(
                recipient.ToString(),
                name,
                payload,
                ct);
        }
        catch (Exception error)
        {
            logger.LogWarning(
                error,
                "Status event delivery failed for {RecipientId}",
                recipient);
        }
    }

    // =====================================================
    // TRANSACTION + AFTER COMMIT
    // =====================================================

    private async Task<T> AtomicAsync<T>(
        Func<List<Func<Task>>, Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false)
    {
        // Do not store events in a Service field.
        // Each operation has its own local collection.
        var afterCommit = new List<Func<Task>>();

        var result =
            await repository.ExecuteAtomicAsync(
                async () =>
                {
                    // Discard events from a rolled-back attempt.
                    afterCommit.Clear();

                    return await operation(afterCommit);
                },
                result =>
                    result is not null &&
                    (result is not bool success || success),
                ct,
                enableRetry);

        // No events for failed business operations.
        if (result is null ||
            result is bool succeeded && !succeeded)
        {
            return result;
        }

        // Commit has completed successfully.
        foreach (var action in afterCommit)
        {
            await action();
        }

        return result;
    }
}

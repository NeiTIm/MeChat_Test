
using Microsoft.Data.SqlClient;

using MeChat.Api.Features.Conversations.Repositories.Interfaces;
using MeChat.Api.Features.Files;
using MeChat.Api.Infrastructure.Persistence.Transactions;
using MeChat.Api.Realtime.Interfaces;

namespace MeChat.Api.Features.Conversations;

public sealed class PrivateChatService(
    TimeProvider clock,
    IRealtimePublisher publisher,
    MessageAttachmentService attachments,
    ILogger<PrivateChatService> logger,
    IPrivateChatRepository repository,
    IUnitOfWork uow)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // PRIVATE CONVERSATION PAIR
    // =====================================================

    private static (Guid First, Guid Second) Pair(
        Guid a,
        Guid b)
    {
        return a.CompareTo(b) < 0
            ? (a, b)
            : (b, a);
    }

    private static bool IsParticipant(
        PrivateConversationPair pair,
        Guid actor)
    {
        return pair.UserId1 == actor ||
               pair.UserId2 == actor;
    }

    private static Guid OtherUser(
        PrivateConversationPair pair,
        Guid actor)
    {
        return pair.UserId1 == actor
            ? pair.UserId2
            : pair.UserId1;
    }

    // =====================================================
    // PREPARE MESSAGE SEND
    // =====================================================

    public async Task<bool> PrepareSendAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        // Must participate in the caller's transaction.
        if (!repository.HasCurrentTransaction)
        {
            throw new InvalidOperationException(
                "Send preparation requires the caller's message transaction.");
        }

        if (!await CanSendOrRequestAsync(actor, id, ct))
            return false;

        if (!await CanSendAsync(actor, id, ct))
        {
            var pair = await repository.GetPairAsync(
                id,
                ct);

            if (pair is null || !IsParticipant(pair, actor))
                return false;

            var receiver = OtherUser(pair, actor);

            var pending =
                await repository.HasPendingRequestAsync(
                    id,
                    actor,
                    receiver,
                    ct);

            if (!pending)
            {
                repository.AddMessageRequest(
                    new MessageRequest
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = id,
                        SenderUserId = actor,
                        ReceiverUserId = receiver,
                        Status = "Pending",
                        CreatedAt = Now
                    });
            }
        }

        return true;
    }

    // =====================================================
    // EXPLICIT NEW MESSAGE REQUEST
    // =====================================================

    public async Task<bool> NewRequestAsync(
        Guid actor,
        Guid id,
        bool confirmed,
        CancellationToken ct)
    {
        if (!confirmed)
            return false;

        await using var tx =
            await uow.BeginSerializableAsync(ct);

        var pair = await repository.GetPairAsync(
            id,
            ct);

        if (pair is null || !IsParticipant(pair, actor))
            return false;

        var receiver = OtherUser(pair, actor);

        if (!await ActivePairAsync(actor, receiver, ct) ||
            await BlockedAsync(actor, receiver, ct) ||
            !await repository.AllowsStrangerMessagesAsync(
                receiver,
                ct))
        {
            return false;
        }

        var access = await repository.GetAccessAsync(
            id,
            actor,
            receiver,
            ct);

        // Explicit requests may renew revoked access,
        // but must not bypass directional rejection.

        if (access?.Status is "Rejected" or "Accepted")
            return false;

        if (await repository.HasPendingRequestAsync(
                id,
                actor,
                receiver,
                ct))
        {
            return true;
        }

        var now = Now;

        if (access is not null)
        {
            access.Status = "Pending";
            access.AcceptedAt = null;
            access.RevokedAt = null;
            access.UpdatedAt = now;
        }

        repository.AddMessageRequest(
            new MessageRequest
            {
                Id = Guid.NewGuid(),
                ConversationId = id,
                SenderUserId = actor,
                ReceiverUserId = receiver,
                Status = "Pending",
                CreatedAt = now
            });

        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return true;
    }

    // =====================================================
    // OPEN PRIVATE CONVERSATION
    // =====================================================

    public async Task<ConversationDto?> OpenAsync(
        Guid actor,
        Guid target,
        CancellationToken ct)
    {
        if (actor == target ||
            !await ActivePairAsync(actor, target, ct) ||
            await BlockedAsync(actor, target, ct))
        {
            return null;
        }

        var (first, second) = Pair(actor, target);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var tx =
                    await uow.BeginSerializableAsync(ct);

                // Recheck inside the transaction.
                if (!await ActivePairAsync(actor, target, ct) ||
                    await BlockedAsync(actor, target, ct))
                {
                    return null;
                }

                var pair =
                    await repository.GetPairByUsersAsync(
                        first,
                        second,
                        ct);

                // =========================================
                // EXISTING CONVERSATION
                // =========================================

                if (pair is not null)
                {
                    var existing =
                        await repository.GetConversationAsync(
                            pair.ConversationId,
                            ct);

                    if (existing is null)
                        return null;

                    return new ConversationDto(
                        existing.Id,
                        existing.Type,
                        existing.Status,
                        target,
                        existing.LastSequence,
                        existing.UpdatedAt);
                }

                // =========================================
                // CREATE CONVERSATION
                // =========================================

                var isFriend = await repository.AreFriendsAsync(
                    first,
                    second,
                    ct);

                var now = Now;

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = "Private",
                    Status = "Active",
                    LastSequence = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                repository.AddConversation(conversation);

                repository.AddPair(
                    new PrivateConversationPair
                    {
                        ConversationId = conversation.Id,
                        UserId1 = first,
                        UserId2 = second
                    });

                // =========================================
                // ACCEPT ACCESS FOR FRIENDS
                // =========================================

                if (isFriend)
                {
                    repository.AddAccess(
                        new PrivateChatAccess
                        {
                            ConversationId = conversation.Id,
                            SenderUserId = actor,
                            ReceiverUserId = target,
                            Status = "Accepted",
                            AcceptedAt = now,
                            UpdatedAt = now
                        });

                    repository.AddAccess(
                        new PrivateChatAccess
                        {
                            ConversationId = conversation.Id,
                            SenderUserId = target,
                            ReceiverUserId = actor,
                            Status = "Accepted",
                            AcceptedAt = now,
                            UpdatedAt = now
                        });
                }

                await uow.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new ConversationDto(
                    conversation.Id,
                    conversation.Type,
                    conversation.Status,
                    target,
                    0,
                    conversation.UpdatedAt);
            }
            catch (Exception error)
                when (Retryable(error) && attempt < 2)
            {
                uow.ClearTracking();

                await Task.Delay(
                    30 * (attempt + 1),
                    ct);
            }
        }

        return null;
    }

    // =====================================================
    // CAN READ PRIVATE CONVERSATION
    // =====================================================

    public async Task<bool> CanReadAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        var pair = await repository.GetPairAsync(id, ct);

        if (pair is null || !IsParticipant(pair, actor))
            return false;

        var other = OtherUser(pair, actor);

        if (!await ActivePairAsync(actor, other, ct) ||
            await BlockedAsync(actor, other, ct))
        {
            return false;
        }

        if (await repository.AreFriendsAsync(
                pair.UserId1,
                pair.UserId2,
                ct))
        {
            return true;
        }

        // Read access accepts an approved grant
        // in either participant direction.

        return await repository.HasAcceptedAccessAsync(
            id,
            actor,
            other,
            eitherDirection: true,
            ct);
    }

    // =====================================================
    // CAN READ MESSAGE REQUEST
    // =====================================================

    public async Task<bool> CanReadRequestAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (!await repository.HasPendingRequestForUserAsync(
                id,
                actor,
                ct))
        {
            return false;
        }

        var pair = await repository.GetPairAsync(id, ct);

        if (pair is null || !IsParticipant(pair, actor))
            return false;

        var other = OtherUser(pair, actor);

        return await ActivePairAsync(actor, other, ct) &&
               !await BlockedAsync(actor, other, ct);
    }

    // =====================================================
    // GET CONVERSATION
    // =====================================================

    public async Task<ConversationDto?> GetAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (!await CanReadAsync(actor, id, ct) &&
            !await CanReadRequestAsync(actor, id, ct) &&
            !await CanSendOrRequestAsync(actor, id, ct))
        {
            return null;
        }

        var pair = await repository.GetPairAsync(id, ct);

        var conversation =
            await repository.GetConversationAsync(id, ct);

        if (pair is null ||
            conversation is null ||
            !IsParticipant(pair, actor))
        {
            return null;
        }

        return new ConversationDto(
            id,
            conversation.Type,
            conversation.Status,
            OtherUser(pair, actor),
            conversation.LastSequence,
            conversation.UpdatedAt);
    }

    // =====================================================
    // LIST PRIVATE CONVERSATIONS
    // =====================================================

    public async Task<List<ConversationDto>> ListAsync(
        Guid actor,
        CancellationToken ct)
    {
        var rows =
            await repository.ListConversationsAsync(
                actor,
                ct);

        var result = new List<ConversationDto>();

        foreach (var row in rows)
        {
            if (!await CanReadAsync(
                    actor,
                    row.ConversationId,
                    ct))
            {
                continue;
            }

            var hidden =
                await repository.IsConversationHiddenAsync(
                    row.ConversationId,
                    actor,
                    row.LastSequence,
                    ct);

            if (hidden)
                continue;

            result.Add(
                new ConversationDto(
                    row.ConversationId,
                    row.Type,
                    row.Status,
                    row.UserId1 == actor
                        ? row.UserId2
                        : row.UserId1,
                    row.LastSequence,
                    row.UpdatedAt));
        }

        return result;
    }

    // =====================================================
    // HIDE CONVERSATION FOR ME
    // =====================================================

    public async Task<bool> HideForMeAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (!await CanReadAsync(actor, id, ct))
            return false;

        await using (var tx =
            await uow.BeginSerializableAsync(ct))
        {
            if (!await CanReadAsync(actor, id, ct))
                return false;

            var conversation =
                await repository.GetConversationAsync(
                    id,
                    ct);

            if (conversation is null)
                return false;

            var state =
                await repository.GetUserStateAsync(
                    id,
                    actor,
                    ct);

            if (state is null)
            {
                state = new ConversationUserState
                {
                    ConversationId = id,
                    UserId = actor
                };

                repository.AddUserState(state);
            }

            state.HiddenThroughSequence = Math.Max(
                state.HiddenThroughSequence,
                conversation.LastSequence);

            state.IsHidden = true;

            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        // Realtime only after commit.
        try
        {
            await publisher.SendToUserAsync(
                actor.ToString(),
                "ConversationHidden",
                new
                {
                    eventId = Guid.NewGuid(),
                    conversationId = id
                },
                ct);
        }
        catch (Exception error)
        {
            logger.LogWarning(
                error,
                "Private hide event delivery failed for {ConversationId}",
                id);
        }

        return true;
    }

    // =====================================================
    // SEND PRIVATE MESSAGE
    // =====================================================

    public async Task<MessageDto?> SendAsync(
        Guid actor,
        Guid id,
        SendMessageRequest input,
        CancellationToken ct)
    {
        // =================================================
        // INPUT VALIDATION
        // =================================================

        if (input.ClientMessageId == Guid.Empty ||
            (
                string.IsNullOrWhiteSpace(input.Content) &&
                input.AttachmentIds is not { Count: > 0 }
            ) ||
            !await CanSendOrRequestAsync(actor, id, ct))
        {
            return null;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Message message;
                Notification? notification = null;
                Guid recipient;

                // =========================================
                // SERIALIZABLE TRANSACTION
                // =========================================

                await using (var tx =
                    await uow.BeginSerializableAsync(ct))
                {
                    // =====================================
                    // RECHECK PERMISSION
                    // =====================================

                    if (!await CanSendOrRequestAsync(
                            actor,
                            id,
                            ct))
                    {
                        return null;
                    }

                    // =====================================
                    // CLIENT MESSAGE IDEMPOTENCY
                    // =====================================

                    var previous =
                        await repository.GetMessageByClientIdAsync(
                            actor,
                            input.ClientMessageId,
                            ct);

                    if (previous is not null)
                    {
                        if (previous.ConversationId != id)
                            return null;

                        if (await repository.IsMessageHiddenAsync(
                                id,
                                actor,
                                previous.Sequence,
                                ct) ||
                            await repository.IsMessageDeletedForUserAsync(
                                previous.Id,
                                actor,
                                ct))
                        {
                            return null;
                        }

                        return await attachments.EnrichAsync(
                            Map(previous),
                            ct);
                    }

                    // =====================================
                    // VALIDATE REPLY
                    // =====================================

                    if (input.ReplyToMessageId is { } replyId)
                    {
                        var reply =
                            await repository.GetMessageAsync(
                                replyId,
                                id,
                                ct);

                        if (reply is null)
                            return null;

                        if (await repository.IsMessageDeletedForUserAsync(
                                replyId,
                                actor,
                                ct))
                        {
                            return null;
                        }

                        var hidden =
                            await repository.GetHiddenThroughSequenceAsync(
                                id,
                                actor,
                                ct);

                        if (reply.Sequence <= hidden)
                            return null;
                    }

                    // =====================================
                    // PREPARE MESSAGE REQUEST
                    // =====================================

                    if (!await PrepareSendAsync(
                            actor,
                            id,
                            ct))
                    {
                        return null;
                    }

                    // =====================================
                    // UPDATE CONVERSATION SEQUENCE
                    // =====================================

                    var conversation =
                        await repository.GetConversationAsync(
                            id,
                            ct);

                    if (conversation is null)
                        return null;

                    conversation.LastSequence++;
                    conversation.UpdatedAt = Now;

                    // =====================================
                    // CREATE MESSAGE
                    // =====================================

                    message = new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = id,
                        SenderUserId = actor,
                        ClientMessageId = input.ClientMessageId,
                        Sequence = conversation.LastSequence,
                        Type = input.AttachmentIds is { Count: > 0 }
                            ? "Media"
                            : "Text",
                        Content = input.Content?.Trim(),
                        ReplyToMessageId = input.ReplyToMessageId,
                        Status = "Sent",
                        CreatedAt = Now
                    };

                    repository.AddMessage(message);

                    // =====================================
                    // BIND ATTACHMENTS
                    // =====================================

                    // MessageAttachmentService does not
                    // call SaveChangesAsync.
                    //
                    // Both services must use the same
                    // scoped AppDbContext.

                    if (!await attachments.BindPendingAsync(
                            actor,
                            message.Id,
                            input.AttachmentIds,
                            ct))
                    {
                        return null;
                    }

                    // =====================================
                    // RECIPIENT
                    // =====================================

                    var pair =
                        await repository.GetPairAsync(id, ct);

                    if (pair is null ||
                        !IsParticipant(pair, actor))
                    {
                        return null;
                    }

                    recipient = OtherUser(pair, actor);

                    // =====================================
                    // NOTIFICATION
                    // =====================================

                    var muted =
                        await repository.IsMutedAsync(
                            id,
                            recipient,
                            Now,
                            ct);

                    if (!muted)
                    {
                        var accepted =
                            await CanSendAsync(actor, id, ct);

                        notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            RecipientUserId = recipient,
                            ActorUserId = actor,
                            Type = accepted
                                ? "Message"
                                : "MessageRequest",
                            ReferenceId = message.Id,
                            CreatedAt = Now
                        };

                        repository.AddNotification(notification);
                    }

                    // =====================================
                    // SAVE AND COMMIT
                    // =====================================

                    await uow.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                }

                // =========================================
                // REALTIME AFTER COMMIT
                // =========================================

                var recipientCanRead =
                    await CanReadAsync(recipient, id, ct) ||
                    await CanReadRequestAsync(recipient, id, ct);

                if (recipientCanRead)
                {
                    try
                    {
                        await publisher.SendToUserAsync(
                            recipient.ToString(),
                            "MessageCreated",
                            new
                            {
                                eventId = Guid.NewGuid(),
                                conversationId = id,
                                messageId = message.Id,
                                sequence = message.Sequence
                            },
                            ct);

                        if (notification is not null)
                        {
                            await publisher.SendToUserAsync(
                                recipient.ToString(),
                                "NotificationCreated",
                                new
                                {
                                    eventId = Guid.NewGuid(),
                                    notificationId = notification.Id,
                                    type = notification.Type,
                                    referenceId = message.Id
                                },
                                ct);
                        }
                    }
                    catch (Exception error)
                    {
                        logger.LogWarning(
                            error,
                            "Private message event delivery failed for {MessageId}",
                            message.Id);
                    }
                }

                // =========================================
                // SENDER REALTIME
                // =========================================

                try
                {
                    await publisher.SendToUserAsync(
                        actor.ToString(),
                        "MessageCreated",
                        new
                        {
                            eventId = Guid.NewGuid(),
                            conversationId = id,
                            messageId = message.Id,
                            sequence = message.Sequence
                        },
                        ct);
                }
                catch (Exception error)
                {
                    logger.LogWarning(
                        error,
                        "Own message event delivery failed for {MessageId}",
                        message.Id);
                }

                // =========================================
                // ENRICH RESPONSE
                // =========================================

                return await attachments.EnrichAsync(
                    Map(message),
                    ct);
            }
            catch (Exception error)
                when (Retryable(error) && attempt < 2)
            {
                uow.ClearTracking();

                await Task.Delay(
                    30 * (attempt + 1),
                    ct);
            }
        }

        return null;
    }

    // =====================================================
    // MESSAGE HISTORY
    // =====================================================

    public async Task<MessagePageDto?> HistoryAsync(
        Guid actor,
        Guid id,
        long? before,
        int limit,
        CancellationToken ct)
    {
        // =============================================
        // AUTHORIZATION BEFORE QUERY
        // =============================================

        if (!await CanReadAsync(actor, id, ct) &&
            !await CanReadRequestAsync(actor, id, ct))
        {
            return null;
        }

        var count = Math.Clamp(limit, 1, 50);

        // =============================================
        // HIDDEN CURSOR
        // =============================================

        var hidden =
            await repository.GetHiddenThroughSequenceAsync(
                id,
                actor,
                ct);

        // =============================================
        // GET HISTORY
        // =============================================

        var messages = await repository.GetHistoryAsync(
            id,
            actor,
            hidden,
            before,
            count + 1,
            ct);

        // =============================================
        // RECHECK AUTHORIZATION
        // =============================================

        if (!await CanReadAsync(actor, id, ct) &&
            !await CanReadRequestAsync(actor, id, ct))
        {
            return null;
        }

        // =============================================
        // PAGINATION
        // =============================================

        var hasMore = messages.Count > count;

        var page = new MessagePageDto(
            messages
                .Take(count)
                .Select(Map)
                .ToList(),
            hasMore
                ? messages[count - 1].Sequence
                : null,
            hasMore);

        return await attachments.EnrichAsync(
            page,
            ct);
    }

    // =====================================================
    // CAN SEND ACCEPTED MESSAGE
    // =====================================================

    private async Task<bool> CanSendAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        var pair = await repository.GetPairAsync(id, ct);

        if (pair is null || !IsParticipant(pair, actor))
            return false;

        var other = OtherUser(pair, actor);

        if (!await ActivePairAsync(actor, other, ct) ||
            await BlockedAsync(actor, other, ct))
        {
            return false;
        }

        if (await repository.AreFriendsAsync(
                pair.UserId1,
                pair.UserId2,
                ct))
        {
            return true;
        }

        // Sending requires a directional grant
        // from the sender to the receiver.

        return await repository.HasAcceptedAccessAsync(
            id,
            actor,
            other,
            eitherDirection: false,
            ct);
    }

    // =====================================================
    // CAN SEND OR REQUEST MESSAGE
    // =====================================================

    private async Task<bool> CanSendOrRequestAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (await CanSendAsync(actor, id, ct))
            return true;

        var pair = await repository.GetPairAsync(id, ct);

        if (pair is null || !IsParticipant(pair, actor))
            return false;

        var receiver = OtherUser(pair, actor);

        if (!await ActivePairAsync(actor, receiver, ct) ||
            await BlockedAsync(actor, receiver, ct))
        {
            return false;
        }

        var access = await repository.GetAccessAsync(
            id,
            actor,
            receiver,
            ct);

        if (access?.Status is "Rejected" or "Revoked")
            return false;

        if (await repository.HasPendingRequestAsync(
                id,
                actor,
                receiver,
                ct))
        {
            return true;
        }

        if (await repository.HasRejectedOrBlockedRequestAsync(
                id,
                actor,
                receiver,
                ct))
        {
            return false;
        }

        return await repository.AllowsStrangerMessagesAsync(
            receiver,
            ct);
    }

    // =====================================================
    // PUBLIC PERMISSION CONTRACTS
    // =====================================================

    public Task<bool> CanSendMessageAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        return CanSendOrRequestAsync(
            actor,
            id,
            ct);
    }

    public Task<bool> CanSendAcceptedAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        return CanSendAsync(
            actor,
            id,
            ct);
    }

    // =====================================================
    // ACTIVE USER PAIR
    // =====================================================

    private async Task<bool> ActivePairAsync(
        Guid a,
        Guid b,
        CancellationToken ct)
    {
        return await repository.CountActiveUsersAsync(
                   a,
                   b,
                   ct) == 2
            && !await repository.HasActiveSuspensionAsync(
                a,
                b,
                Now,
                ct);
    }

    // =====================================================
    // BLOCK STATUS
    // =====================================================

    private Task<bool> BlockedAsync(
        Guid a,
        Guid b,
        CancellationToken ct)
    {
        return repository.IsBlockedAsync(
            a,
            b,
            ct);
    }

    // =====================================================
    // ENTITY -> MESSAGE DTO
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
    // RETRYABLE SQL SERVER ERRORS
    // =====================================================

    private static bool Retryable(Exception error)
    {
        for (
            Exception? current = error;
            current is not null;
            current = current.InnerException)
        {
            if (current is SqlException sql &&
                sql.Number is 1205 or 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}

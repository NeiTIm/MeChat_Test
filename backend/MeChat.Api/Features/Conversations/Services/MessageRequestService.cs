
using MeChat.Api.Features.Conversations.Repositories.Interfaces;
using MeChat.Api.Realtime;

namespace MeChat.Api.Features.Conversations;

public sealed class MessageRequestService(
    IMessageRequestRepository repository,
    TimeProvider clock,
    PrivateChatService chats,
    StateEvents events)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // INBOX
    // =====================================================

    public async Task<List<MessageRequestDto>> InboxAsync(
        Guid actor,
        CancellationToken ct)
    {
        var requests =
            await repository.GetPendingInboxAsync(
                actor,
                ct);

        var result = new List<MessageRequestDto>();

        foreach (var request in requests)
        {
            // Preserve the existing permission check.
            var canRead = await chats.CanReadRequestAsync(
                actor,
                request.ConversationId,
                ct);

            if (!canRead)
                continue;

            result.Add(request);
        }

        return result;
    }

    // =====================================================
    // UNREAD COUNT
    // =====================================================

    public async Task<int> UnreadCountAsync(
        Guid actor,
        CancellationToken ct)
    {
        var requests = await InboxAsync(
            actor,
            ct);

        return requests.Count(
            request =>
                request.LastSequence >
                request.RequestReadSequence);
    }

    // =====================================================
    // RESPOND TO MESSAGE REQUEST
    // =====================================================

    public Task<bool> RespondAsync(
        Guid actor,
        Guid requestId,
        bool accept,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var request =
                    await repository
                        .GetPendingRequestForUpdateAsync(
                            requestId,
                            actor,
                            ct);

                if (request is null)
                    return false;

                // Authorization must remain in the Service.
                var canRead =
                    await chats.CanReadRequestAsync(
                        actor,
                        request.ConversationId,
                        ct);

                if (!canRead)
                    return false;

                var now = Now;

                // =================================================
                // UPDATE MESSAGE REQUEST
                // =================================================

                request.Status =
                    accept ? "Accepted" : "Rejected";

                request.RespondedAt = now;

                // =================================================
                // SENDER -> RECEIVER ACCESS
                // =================================================

                var access =
                    await repository.GetPrivateChatAccessAsync(
                        request.ConversationId,
                        request.SenderUserId,
                        actor,
                        ct);

                if (access is null)
                {
                    access = new PrivateChatAccess
                    {
                        ConversationId = request.ConversationId,
                        SenderUserId = request.SenderUserId,
                        ReceiverUserId = actor
                    };

                    repository.AddPrivateChatAccess(access);
                }

                access.Status =
                    accept ? "Accepted" : "Rejected";

                access.AcceptedAt =
                    accept ? now : null;

                access.RejectedAt =
                    accept ? null : now;

                access.UpdatedAt = now;

                // =================================================
                // RECEIVER -> SENDER ACCESS
                // =================================================

                if (accept)
                {
                    var reverse =
                        await repository.GetPrivateChatAccessAsync(
                            request.ConversationId,
                            actor,
                            request.SenderUserId,
                            ct);

                    if (reverse is null)
                    {
                        reverse = new PrivateChatAccess
                        {
                            ConversationId =
                                request.ConversationId,

                            SenderUserId = actor,

                            ReceiverUserId =
                                request.SenderUserId
                        };

                        repository.AddPrivateChatAccess(reverse);
                    }

                    reverse.Status = "Accepted";
                    reverse.AcceptedAt = now;
                    reverse.RejectedAt = null;
                    reverse.UpdatedAt = now;
                }

                // =================================================
                // SAVE
                // =================================================

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME - ONLY AFTER COMMIT
                // =================================================

                var conversationId = request.ConversationId;
                var senderUserId = request.SenderUserId;
                var status = request.Status;

                afterCommit.Add(() =>
                    events.SendAsync(
                        [actor, senderUserId],
                        "MessageRequestChanged",
                        new
                        {
                            eventId = Guid.NewGuid(),
                            conversationId,
                            kind = "Status",
                            status
                        },
                        ct));

                return true;
            },
            ct);
    }

    // =====================================================
    // MARK REQUEST AS READ
    // =====================================================

    public Task<bool> MarkReadAsync(
        Guid actor,
        Guid requestId,
        long sequence,
        CancellationToken ct)
    {
        if (sequence < 0)
            return Task.FromResult(false);

        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var request =
                    await repository
                        .GetPendingRequestForUpdateAsync(
                            requestId,
                            actor,
                            ct);

                if (request is null)
                    return false;

                // =================================================
                // AUTHORIZATION
                // =================================================

                var canRead =
                    await chats.CanReadRequestAsync(
                        actor,
                        request.ConversationId,
                        ct);

                if (!canRead)
                    return false;

                // =================================================
                // VALIDATE SEQUENCE
                // =================================================

                var lastSequence =
                    await repository.GetLastSequenceAsync(
                        request.ConversationId,
                        ct);

                if (lastSequence is null ||
                    sequence > lastSequence.Value)
                {
                    return false;
                }

                // =================================================
                // GET OR CREATE USER STATE
                // =================================================

                var state =
                    await repository
                        .GetConversationUserStateAsync(
                            request.ConversationId,
                            actor,
                            ct);

                if (state is null)
                {
                    state = new ConversationUserState
                    {
                        ConversationId =
                            request.ConversationId,

                        UserId = actor
                    };

                    repository.AddConversationUserState(state);
                }

                // =================================================
                // MONOTONIC READ CURSOR
                // =================================================

                var changed =
                    sequence > state.RequestReadSequence;

                if (changed)
                {
                    state.RequestReadSequence = sequence;
                    state.RequestReadAt = Now;
                }

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME - ONLY IF CURSOR ADVANCED
                // =================================================

                if (changed)
                {
                    var conversationId =
                        request.ConversationId;

                    afterCommit.Add(() =>
                        events.SendAsync(
                            [actor],
                            "MessageRequestChanged",
                            new
                            {
                                eventId = Guid.NewGuid(),
                                conversationId,
                                kind = "Read"
                            },
                            ct));
                }

                return true;
            },
            ct);
    }

    // =====================================================
    // ATOMIC OPERATION + AFTER COMMIT ACTIONS
    // =====================================================

    private async Task<T> AtomicAsync<T>(
        Func<List<Func<Task>>, Task<T>> operation,
        CancellationToken ct)
    {
        // Keep side effects local to each operation.
        // Do not use a shared class-level collection.
        var afterCommit = new List<Func<Task>>();

        var result =
            await repository.ExecuteAtomicAsync(
                async () =>
                {
                    // An earlier attempt may have rolled back.
                    // Its realtime events must not be published.
                    afterCommit.Clear();

                    return await operation(afterCommit);
                },
                ct);

        // Repository has committed successfully.
        // Realtime events can now be delivered.
        foreach (var action in afterCommit)
        {
            await action();
        }

        return result;
    }
}

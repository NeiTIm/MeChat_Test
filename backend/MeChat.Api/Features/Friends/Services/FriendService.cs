
using MeChat.Api.Features.Friends.Repositories.Interfaces;
using MeChat.Api.Realtime;

namespace MeChat.Api.Features.Friends;

public sealed class FriendService(
    IFriendRepository repository,
    TimeProvider clock,
    StateEvents events)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // NORMALIZED USER PAIR
    // =====================================================

    private static (Guid First, Guid Second) Pair(
        Guid a,
        Guid b)
    {
        return a.CompareTo(b) < 0
            ? (a, b)
            : (b, a);
    }

    // =====================================================
    // SEND FRIEND REQUEST
    // =====================================================

    public Task<FriendActionResult> SendAsync(
        Guid actor,
        Guid target,
        CancellationToken ct)
    {
        if (actor == target || target == Guid.Empty)
        {
            return Task.FromResult(
                FriendActionResult.Forbidden);
        }

        return AtomicAsync<FriendActionResult>(
            async afterCommit =>
            {
                // =================================================
                // ELIGIBILITY
                // =================================================

                if (!await EligiblePairAsync(
                        actor, target, ct))
                {
                    return FriendActionResult.Forbidden;
                }

                var (first, second) = Pair(actor, target);

                // =================================================
                // EXISTING FRIENDSHIP
                // =================================================

                var alreadyFriends =
                    await repository.HasActiveFriendshipAsync(
                        first,
                        second,
                        ct);

                if (alreadyFriends)
                {
                    return FriendActionResult.Conflict;
                }

                // =================================================
                // EXISTING PENDING REQUEST
                // =================================================

                var pending =
                    await repository.GetPendingPairRequestAsync(
                        first,
                        second,
                        ct);

                if (pending is not null)
                {
                    // Same sender cannot send twice.
                    if (pending.SenderUserId == actor)
                    {
                        return FriendActionResult.Conflict;
                    }

                    // Reverse friend request automatically accepts.
                    var now = Now;

                    pending.Status = "Accepted";
                    pending.RespondedAt = now;

                    repository.AddFriendship(
                        new Friendship
                        {
                            Id = Guid.NewGuid(),
                            UserId1 = first,
                            UserId2 = second,
                            CreatedAt = now
                        });

                    await PreserveChatGrantAsync(
                        first,
                        second,
                        ct);

                    await repository.SaveChangesAsync(ct);

                    afterCommit.Add(() =>
                        ChangedAsync(actor, target, ct));

                    return FriendActionResult.Accepted;
                }

                // =================================================
                // CREATE NEW REQUEST
                // =================================================

                repository.AddFriendRequest(
                    new FriendRequest
                    {
                        Id = Guid.NewGuid(),
                        SenderUserId = actor,
                        ReceiverUserId = target,
                        PairUserId1 = first,
                        PairUserId2 = second,
                        Status = "Pending",
                        CreatedAt = Now
                    });

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    ChangedAsync(actor, target, ct));

                return FriendActionResult.Created;
            },
            ct,
            enableRetry: true);
    }

    // =====================================================
    // RESPOND TO FRIEND REQUEST
    // =====================================================

    public Task<FriendActionResult> RespondAsync(
        Guid actor,
        Guid requestId,
        bool accept,
        CancellationToken ct)
    {
        return AtomicAsync<FriendActionResult>(
            async afterCommit =>
            {
                var request =
                    await repository.GetPendingReceivedRequestAsync(
                        requestId,
                        actor,
                        ct);

                if (request is null)
                {
                    return FriendActionResult.NotFound;
                }

                // =================================================
                // ELIGIBILITY
                // =================================================

                if (!await EligiblePairAsync(
                        request.SenderUserId,
                        actor,
                        ct))
                {
                    return FriendActionResult.Forbidden;
                }

                var now = Now;

                request.Status =
                    accept ? "Accepted" : "Rejected";

                request.RespondedAt = now;

                // =================================================
                // ACCEPT FRIEND REQUEST
                // =================================================

                if (accept)
                {
                    var exists =
                        await repository.HasActiveFriendshipAsync(
                            request.PairUserId1,
                            request.PairUserId2,
                            ct);

                    if (!exists)
                    {
                        repository.AddFriendship(
                            new Friendship
                            {
                                Id = Guid.NewGuid(),
                                UserId1 = request.PairUserId1,
                                UserId2 = request.PairUserId2,
                                CreatedAt = now
                            });
                    }

                    // Preserve chat permissions independently
                    // of the friendship record.
                    await PreserveChatGrantAsync(
                        request.PairUserId1,
                        request.PairUserId2,
                        ct);
                }

                await repository.SaveChangesAsync(ct);

                var senderUserId = request.SenderUserId;

                afterCommit.Add(() =>
                    ChangedAsync(actor, senderUserId, ct));

                // Preserve original result contract.
                return accept
                    ? FriendActionResult.Accepted
                    : FriendActionResult.Created;
            },
            ct);
    }

    // =====================================================
    // CANCEL FRIEND REQUEST
    // =====================================================

    public Task<bool> CancelAsync(
        Guid actor,
        Guid requestId,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var request =
                    await repository.GetPendingSentRequestAsync(
                        requestId,
                        actor,
                        ct);

                if (request is null)
                    return false;

                request.Status = "Cancelled";
                request.RespondedAt = Now;

                await repository.SaveChangesAsync(ct);

                var receiverUserId = request.ReceiverUserId;

                afterCommit.Add(() =>
                    ChangedAsync(actor, receiverUserId, ct));

                return true;
            },
            ct);
    }

    // =====================================================
    // UNFRIEND
    // =====================================================

    public Task<bool> UnfriendAsync(
        Guid actor,
        Guid target,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var (first, second) = Pair(actor, target);

                var friendship =
                    await repository.GetActiveFriendshipAsync(
                        first,
                        second,
                        ct);

                if (friendship is null)
                    return false;

                // Keep existing private conversation grants.
                await PreserveChatGrantAsync(
                    first,
                    second,
                    ct);

                friendship.RemovedAt = Now;

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    ChangedAsync(actor, target, ct));

                return true;
            },
            ct);
    }

    // =====================================================
    // BLOCK USER
    // =====================================================

    public async Task<bool> BlockAsync(
        Guid actor,
        Guid target,
        CancellationToken ct)
    {
        if (actor == target || target == Guid.Empty)
            return false;

        return await AtomicAsync<bool>(
            async afterCommit =>
            {
                var targetExists =
                    await repository.UserExistsAsync(
                        target,
                        ct);

                if (!targetExists)
                    return false;

                // =================================================
                // EXISTING BLOCK
                // =================================================

                var existingBlock =
                    await repository.GetActiveUserBlockAsync(
                        actor,
                        target,
                        ct);

                if (existingBlock is not null)
                {
                    // Already blocked: idempotent success.
                    return true;
                }

                var now = Now;

                // =================================================
                // CREATE BLOCK
                // =================================================

                repository.AddUserBlock(
                    new UserBlock
                    {
                        Id = Guid.NewGuid(),
                        BlockerUserId = actor,
                        BlockedUserId = target,
                        CreatedAt = now
                    });

                var (first, second) = Pair(actor, target);

                // =================================================
                // REMOVE ACTIVE FRIENDSHIP
                // =================================================

                var friendship =
                    await repository.GetActiveFriendshipAsync(
                        first,
                        second,
                        ct);

                if (friendship is not null)
                {
                    friendship.RemovedAt = now;
                }

                // =================================================
                // BLOCK PENDING FRIEND REQUESTS
                // =================================================

                var pendingFriendRequests =
                    await repository.GetPendingPairRequestsAsync(
                        first,
                        second,
                        ct);

                foreach (var request in pendingFriendRequests)
                {
                    request.Status = "Blocked";
                    request.RespondedAt = now;
                }

                // =================================================
                // REVOKE PRIVATE CHAT ACCESS
                // =================================================

                var accesses =
                    await repository.GetPairChatAccessesAsync(
                        actor,
                        target,
                        ct);

                foreach (var access in accesses)
                {
                    access.Status = "Revoked";
                    access.RevokedAt = now;
                    access.UpdatedAt = now;
                }

                // =================================================
                // BLOCK PENDING MESSAGE REQUESTS
                // =================================================

                var pendingMessageRequests =
                    await repository.GetPendingPairMessageRequestsAsync(
                        actor,
                        target,
                        ct);

                foreach (var request in pendingMessageRequests)
                {
                    request.Status = "Blocked";
                    request.RespondedAt = now;
                }

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME
                // =================================================

                afterCommit.Add(() =>
                    events.SendAsync(
                        [actor, target],
                        "PermissionsChanged",
                        new
                        {
                            eventId = Guid.NewGuid()
                        },
                        ct));

                return true;
            },
            ct);
    }

    // =====================================================
    // UNBLOCK USER
    // =====================================================

    public Task<bool> UnblockAsync(
        Guid actor,
        Guid target,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var block =
                    await repository.GetActiveUserBlockAsync(
                        actor,
                        target,
                        ct);

                if (block is null)
                    return false;

                block.UnblockedAt = Now;

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    ChangedAsync(actor, target, ct));

                // Do not automatically restore friendship.
                return true;
            },
            ct);
    }

    // =====================================================
    // FRIENDS LIST
    // =====================================================

    public Task<List<FriendDto>> FriendsAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.GetFriendsAsync(
            actor,
            ct);
    }

    // =====================================================
    // BLOCKED USERS
    // =====================================================

    public Task<List<BlockedUserDto>> BlocksAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.GetBlockedUsersAsync(
            actor,
            ct);
    }

    // =====================================================
    // FRIEND REQUESTS
    // =====================================================

    public Task<FriendRequestsDto> RequestsAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.GetFriendRequestsAsync(
            actor,
            ct);
    }

    // =====================================================
    // ELIGIBLE PAIR BUSINESS RULE
    // =====================================================

    private async Task<bool> EligiblePairAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        // Both accounts must exist, be verified,
        // and not be deactivated.

        var eligibleUsers =
            await repository.CountEligibleUsersAsync(
                firstUserId,
                secondUserId,
                ct);

        if (eligibleUsers != 2)
            return false;

        // Neither user may have an active suspension.

        var suspended =
            await repository.HasActiveSuspensionAsync(
                firstUserId,
                secondUserId,
                Now,
                ct);

        if (suspended)
            return false;

        // No active block in either direction.

        var blocked =
            await repository.HasActiveBlockAsync(
                firstUserId,
                secondUserId,
                ct);

        return !blocked;
    }

    // =====================================================
    // PRESERVE PRIVATE CHAT GRANTS
    // =====================================================

    private async Task PreserveChatGrantAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        // Preserve a chat grant independently of the
        // Friendship entity, including conversations
        // that existed before the friendship.

        var pair =
            await repository.GetPrivateConversationPairAsync(
                firstUserId,
                secondUserId,
                ct);

        if (pair is null)
            return;

        var now = Now;

        // =================================================
        // BOTH DIRECTIONS
        // =================================================

        var directions = new[]
        {
            (Sender: firstUserId, Receiver: secondUserId),
            (Sender: secondUserId, Receiver: firstUserId)
        };

        foreach (var (sender, receiver) in directions)
        {
            var access =
                await repository.GetPrivateChatAccessAsync(
                    pair.ConversationId,
                    sender,
                    receiver,
                    ct);

            if (access is null)
            {
                access = new PrivateChatAccess
                {
                    ConversationId = pair.ConversationId,
                    SenderUserId = sender,
                    ReceiverUserId = receiver
                };

                repository.AddPrivateChatAccess(access);
            }

            access.Status = "Accepted";
            access.AcceptedAt = now;
            access.RejectedAt = null;
            access.RevokedAt = null;
            access.UpdatedAt = now;
        }

        // =================================================
        // ACCEPT PENDING MESSAGE REQUESTS
        // =================================================

        var pending =
            await repository.GetPendingConversationRequestsAsync(
                pair.ConversationId,
                ct);

        foreach (var request in pending)
        {
            request.Status = "Accepted";
            request.RespondedAt = now;
        }
    }

    // =====================================================
    // REALTIME FRIENDSHIP EVENT
    // =====================================================

    private Task ChangedAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct)
    {
        return events.SendAsync(
            [firstUserId, secondUserId],
            "FriendshipChanged",
            new
            {
                eventId = Guid.NewGuid()
            },
            ct);
    }

    // =====================================================
    // TRANSACTION + AFTER COMMIT
    // =====================================================

    private async Task<T> AtomicAsync<T>(
        Func<List<Func<Task>>, Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false)
    {
        // Local to this operation; never share between requests.
        var afterCommit = new List<Func<Task>>();

        var result =
            await repository.ExecuteAtomicAsync(
                async () =>
                {
                    // A failed retry must not publish
                    // realtime events from an earlier attempt.
                    afterCommit.Clear();

                    return await operation(afterCommit);
                },
                ct,
                enableRetry);

        // Only publish after SQL transaction commits.
        foreach (var action in afterCommit)
        {
            await action();
        }

        return result;
    }
}


using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Files;
using MeChat.Api.Features.Groups.Repositories.Interfaces;
using MeChat.Api.Realtime;
using MeChat.Api.Realtime.Interfaces;

namespace MeChat.Api.Features.Groups;

public sealed class GroupService(
    IGroupRepository repository,
    TimeProvider clock,
    IRealtimePublisher publisher,
    MessageAttachmentService attachments,
    ILogger<GroupService> logger,
    StateEvents events)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // CREATE GROUP
    // =====================================================

    public async Task<GroupDto?> CreateAsync(
        Guid actor,
        CreateGroupRequest input,
        CancellationToken ct)
    {
        var name = input.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 100 ||
            !await ActiveUserAsync(actor, ct))
        {
            return null;
        }

        var invitees = (input.InviteeUserIds ?? [])
            .Where(id => id != actor)
            .Distinct()
            .ToArray();

        if (invitees.Length > 99)
            return null;

        return await AtomicAsync<GroupDto?>(
            async afterCommit =>
            {
                var now = Now;

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = "Group",
                    Status = "Active",
                    LastSequence = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                repository.AddConversation(conversation);

                repository.AddGroupProfile(
                    new GroupProfile
                    {
                        ConversationId = conversation.Id,
                        Name = name,
                        Description = input.Description?.Trim(),
                        CreatedByUserId = actor,
                        CreatedAt = now
                    });

                repository.AddMember(
                    new ConversationMember
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = actor,
                        Role = "Owner",
                        JoinSequence = 0,
                        JoinedAt = now
                    });

                // Validate invitees within the transaction.
                foreach (var invitee in invitees)
                {
                    if (!await CanInviteUserAsync(
                            actor, invitee, ct))
                        return null;

                    repository.AddInvitation(
                        new GroupInvitation
                        {
                            Id = Guid.NewGuid(),
                            ConversationId = conversation.Id,
                            InviterUserId = actor,
                            InviteeUserId = invitee,
                            Status = "Pending",
                            CreatedAt = now
                        });
                }

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    events.GroupChangedAsync(
                        conversation.Id, ct));

                afterCommit.Add(() =>
                    events.SendAsync(
                        invitees,
                        "GroupInvitationChanged",
                        new
                        {
                            eventId = Guid.NewGuid(),
                            conversationId = conversation.Id
                        },
                        ct));

                return new GroupDto(
                    conversation.Id,
                    name,
                    input.Description?.Trim(),
                    "Active",
                    actor,
                    0,
                    "Owner");
            },
            ct);
    }

    // =====================================================
    // LIST GROUPS
    // =====================================================

    public Task<List<GroupDto>> ListAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.GetGroupsAsync(actor, ct);
    }

    // =====================================================
    // GET GROUP DETAILS
    // =====================================================

    public async Task<GroupDto?> GetAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (!await CanReadAsync(actor, id, ct))
            return null;

        return await repository.GetGroupAsync(
            actor, id, ct);
    }

    // =====================================================
    // GROUP MEMBERS
    // =====================================================

    public async Task<List<GroupMemberDto>?> MembersAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (!await CanSendAsync(actor, id, ct))
            return null;

        return await repository.GetMembersAsync(id, ct);
    }

    // =====================================================
    // INVITE USER
    // =====================================================

    public async Task<bool> InviteAsync(
        Guid actor,
        Guid id,
        Guid invitee,
        CancellationToken ct)
    {
        if (!await CanSendAsync(actor, id, ct) ||
            !await CanInviteUserAsync(actor, invitee, ct))
        {
            return false;
        }

        return await AtomicAsync<bool>(
            async afterCommit =>
            {
                // Repeat authorization inside transaction.
                if (!await CanSendAsync(actor, id, ct) ||
                    !await CanInviteUserAsync(actor, invitee, ct))
                {
                    return false;
                }

                if (await repository.HasMemberAsync(
                        id, invitee, true, ct))
                    return false;

                if (await repository.HasPendingInvitationAsync(
                        id, invitee, ct))
                    return false;

                repository.AddInvitation(
                    new GroupInvitation
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = id,
                        InviterUserId = actor,
                        InviteeUserId = invitee,
                        Status = "Pending",
                        CreatedAt = Now
                    });

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    events.SendAsync(
                        [invitee],
                        "GroupInvitationChanged",
                        new
                        {
                            eventId = Guid.NewGuid(),
                            conversationId = id
                        },
                        ct));

                return true;
            },
            ct,
            enableRetry: true);
    }

    // =====================================================
    // INCOMING GROUP INVITATIONS
    // =====================================================

    public Task<List<GroupInvitationDto>> InvitationsAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.GetInvitationsAsync(actor, ct);
    }

    // =====================================================
    // RESPOND TO INVITATION
    // =====================================================

    public Task<bool> RespondInvitationAsync(
        Guid actor,
        Guid invitationId,
        bool accept,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var invitation =
                    await repository.GetPendingInvitationAsync(
                        invitationId, actor, ct);

                if (invitation is null)
                    return false;

                var conversation =
                    await repository.GetActiveGroupAsync(
                        invitation.ConversationId, ct);

                if (conversation is null ||
                    !await ActiveUserAsync(actor, ct))
                {
                    return false;
                }

                if (accept)
                {
                    if (await BlockedAsync(
                            actor, invitation.InviterUserId, ct))
                        return false;

                    if (await repository.HasMemberAsync(
                            conversation.Id, actor, true, ct))
                        return false;

                    var count =
                        await repository.CountActiveMembersAsync(
                            conversation.Id, ct);

                    if (count >= 100)
                        return false;

                    var lastRemoval =
                        await repository.GetLastRemovalAsync(
                            conversation.Id, actor, ct);

                    if (lastRemoval is not null &&
                        invitation.CreatedAt <= lastRemoval)
                    {
                        return false;
                    }

                    repository.AddMember(
                        new ConversationMember
                        {
                            Id = Guid.NewGuid(),
                            ConversationId = conversation.Id,
                            UserId = actor,
                            Role = "Member",
                            JoinSequence =
                                conversation.LastSequence + 1,
                            JoinedAt = Now
                        });
                }

                invitation.Status =
                    accept ? "Accepted" : "Rejected";

                invitation.RespondedAt = Now;

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    events.GroupChangedAsync(
                        conversation.Id,
                        ct,
                        actor));

                return true;
            },
            ct,
            enableRetry: true);
    }

    // =====================================================
    // LEAVE GROUP
    // =====================================================

    public Task<bool> LeaveAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                var member =
                    await repository.GetActiveMemberAsync(
                        id, actor, ct);

                var conversation =
                    await repository.GetActiveGroupAsync(
                        id, ct);

                if (member is null ||
                    member.Role == "Owner" ||
                    conversation is null)
                {
                    return false;
                }

                member.LeftAt = Now;
                member.LeaveSequence = conversation.LastSequence;
                member.LeftReason = "Left";

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(() =>
                    events.GroupChangedAsync(
                        id, ct, actor));

                return true;
            },
            ct);
    }

    // =====================================================
    // CAN READ GROUP
    // =====================================================

    public async Task<bool> CanReadAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (!await repository.GroupExistsAsync(
                id, true, ct))
            return false;

        if (!await repository.HasMemberAsync(
                id, actor, false, ct))
            return false;

        return await ActiveUserAsync(actor, ct);
    }

    // =====================================================
    // CAN SEND GROUP MESSAGE
    // =====================================================

    public async Task<bool> CanSendAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        if (!await repository.GroupExistsAsync(
                id, false, ct))
            return false;

        if (!await repository.HasMemberAsync(
                id, actor, true, ct))
            return false;

        return await ActiveUserAsync(actor, ct);
    }

    // =====================================================
    // CAN VIEW MESSAGE
    // =====================================================

    public async Task<bool> CanViewMessageAsync(
        Guid actor,
        Guid id,
        long sequence,
        CancellationToken ct)
    {
        if (!await CanReadAsync(actor, id, ct))
            return false;

        if (await repository.IsSequenceHiddenAsync(
                actor, id, sequence, ct))
            return false;

        return await repository.CanAccessSequenceAsync(
            actor, id, sequence, ct);
    }

    // =====================================================
    // HIDE CONVERSATION FOR ME
    // =====================================================

    public Task<bool> HideForMeAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(
            async afterCommit =>
            {
                if (!await CanReadAsync(actor, id, ct))
                    return false;

                var conversation =
                    await repository.GetConversationAsync(id, ct);

                if (conversation is null ||
                    conversation.Status != "Active")
                {
                    return false;
                }

                var state =
                    await repository.GetUserStateAsync(
                        id, actor, ct);

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

                await repository.SaveChangesAsync(ct);

                afterCommit.Add(async () =>
                {
                    try
                    {
                        await publisher.SendToUserAsync(
                            actor.ToString(),
                            "ConversationHidden",
                            new { conversationId = id },
                            ct);
                    }
                    catch (Exception error)
                    {
                        logger.LogWarning(
                            error,
                            "Group hide event delivery failed for {ConversationId}",
                            id);
                    }
                });

                return true;
            },
            ct);
    }

    // =====================================================
    // SEND GROUP MESSAGE
    // =====================================================

    public async Task<MessageDto?> SendAsync(
        Guid actor,
        Guid id,
        SendMessageRequest input,
        CancellationToken ct)
    {
        if (input.ClientMessageId == Guid.Empty ||
            (string.IsNullOrWhiteSpace(input.Content) &&
             input.AttachmentIds is not { Count: > 0 }) ||
            !await CanSendAsync(actor, id, ct))
        {
            return null;
        }

        var outcome = await AtomicAsync<SendOutcome?>(
            async afterCommit =>
            {
                if (!await CanSendAsync(actor, id, ct))
                    return null;

                // =================================================
                // IDEMPOTENCY
                // =================================================

                var previous =
                    await repository.GetMessageByClientIdAsync(
                        actor, input.ClientMessageId, ct);

                if (previous is not null)
                {
                    if (previous.ConversationId != id ||
                        !await CanViewMessageAsync(
                            actor, id, previous.Sequence, ct) ||
                        await repository.IsMessageDeletedForUserAsync(
                            previous.Id, actor, ct))
                    {
                        return null;
                    }

                    // Do not publish duplicate events.
                    return new SendOutcome(previous);
                }

                // =================================================
                // REPLY VALIDATION
                // =================================================

                if (input.ReplyToMessageId is { } replyId)
                {
                    var reply =
                        await repository.GetReplyMessageAsync(
                            id, replyId, ct);

                    if (reply is null ||
                        !await CanViewMessageAsync(
                            actor, id, reply.Sequence, ct) ||
                        await repository.IsMessageDeletedForUserAsync(
                            replyId, actor, ct))
                    {
                        return null;
                    }
                }

                // =================================================
                // CONVERSATION SEQUENCE
                // =================================================

                var conversation =
                    await repository.GetActiveGroupAsync(id, ct);

                if (conversation is null)
                    return null;

                var now = Now;

                conversation.LastSequence++;
                conversation.UpdatedAt = now;

                // =================================================
                // CREATE MESSAGE
                // =================================================

                var message = new Message
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
                    CreatedAt = now
                };

                repository.AddMessage(message);

                // IMPORTANT:
                // Attachment binding must use the same scoped
                // DbContext and database transaction.
                var bound = await attachments.BindPendingAsync(
                    actor,
                    message.Id,
                    input.AttachmentIds,
                    ct);

                if (!bound)
                    return null;

                // =================================================
                // RECIPIENTS / MUTED MEMBERS
                // =================================================

                var recipients =
                    await repository.GetActiveMemberIdsAsync(
                        id, ct);

                var mutedRecipients =
                    await repository.GetMutedMemberIdsAsync(
                        id, now, ct);

                var mutedSet = mutedRecipients.ToHashSet();

                var notifications =
                    new Dictionary<Guid, Notification>();

                foreach (var recipient in recipients)
                {
                    if (recipient == actor ||
                        mutedSet.Contains(recipient))
                    {
                        continue;
                    }

                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        RecipientUserId = recipient,
                        ActorUserId = actor,
                        Type = "GroupMessage",
                        ReferenceId = message.Id,
                        CreatedAt = now
                    };

                    repository.AddNotification(notification);
                    notifications[recipient] = notification;
                }

                await repository.SaveChangesAsync(ct);

                // =================================================
                // REALTIME AFTER COMMIT
                // =================================================

                afterCommit.Add(async () =>
                {
                    foreach (var recipient in recipients)
                    {
                        if (!await CanSendAsync(
                                recipient, id, ct))
                        {
                            continue;
                        }

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

                            if (notifications.TryGetValue(
                                    recipient, out var notification))
                            {
                                await publisher.SendToUserAsync(
                                    recipient.ToString(),
                                    "NotificationCreated",
                                    new
                                    {
                                        eventId = Guid.NewGuid(),
                                        notificationId =
                                            notification.Id,
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
                                "Group message event delivery failed for {MessageId}",
                                message.Id);
                        }
                    }
                });

                return new SendOutcome(message);
            },
            ct,
            enableRetry: true);

        if (outcome is null)
            return null;

        return await attachments.EnrichAsync(
            Map(outcome.Message), ct);
    }

    // =====================================================
    // GROUP MESSAGE HISTORY
    // =====================================================

    public async Task<MessagePageDto?> HistoryAsync(
        Guid actor,
        Guid id,
        long? before,
        int limit,
        CancellationToken ct)
    {
        if (!await CanReadAsync(actor, id, ct))
            return null;

        var count = Math.Clamp(limit, 1, 50);

        var messages =
            await repository.GetVisibleMessagesAsync(
                actor,
                id,
                before,
                count + 1,
                ct);

        // Recheck permission after reading.
        if (!await CanReadAsync(actor, id, ct))
            return null;

        var hasMore = messages.Count > count;

        var nextCursor = hasMore
            ? messages[count - 1].Sequence
            : (long?)null;

        var page = new MessagePageDto(
            messages.Take(count).Select(Map).ToList(),
            nextCursor,
            hasMore);

        return await attachments.EnrichAsync(page, ct);
    }

    // =====================================================
    // USER ELIGIBILITY
    // =====================================================

    private Task<bool> ActiveUserAsync(
        Guid userId,
        CancellationToken ct)
    {
        return repository.IsActiveUserAsync(
            userId, Now, ct);
    }

    private Task<bool> BlockedAsync(
        Guid first,
        Guid second,
        CancellationToken ct)
    {
        return repository.IsBlockedBetweenAsync(
            first, second, ct);
    }

    private async Task<bool> CanInviteUserAsync(
        Guid inviter,
        Guid invitee,
        CancellationToken ct)
    {
        if (inviter == invitee)
            return false;

        if (!await ActiveUserAsync(invitee, ct))
            return false;

        if (!await repository.AllowsGroupInvitesAsync(
                invitee, ct))
            return false;

        return !await BlockedAsync(
            inviter, invitee, ct);
    }

    // =====================================================
    // MESSAGE MAPPING
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

    private sealed record SendOutcome(Message Message);

    // =====================================================
    // TRANSACTION + AFTER COMMIT
    // =====================================================

    private async Task<T> AtomicAsync<T>(
        Func<List<Func<Task>>, Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false)
    {
        // Scoped to one operation, not a shared field.
        var afterCommit = new List<Func<Task>>();

        var result = await repository.ExecuteAtomicAsync(
            async () =>
            {
                // Discard events from a rolled-back retry.
                afterCommit.Clear();

                return await operation(afterCommit);
            },
            ct,
            enableRetry);

        // Database transaction has committed.
        foreach (var action in afterCommit)
        {
            await action();
        }

        return result;
    }
}

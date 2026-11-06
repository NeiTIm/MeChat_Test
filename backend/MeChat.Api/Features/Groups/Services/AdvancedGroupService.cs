
using MeChat.Api.Features.Groups.Repositories.Interfaces;
using MeChat.Api.Realtime;

namespace MeChat.Api.Features.Groups;

public sealed class AdvancedGroupService(
    IAdvancedGroupRepository repository,
    GroupService groups,
    TimeProvider clock,
    StateEvents events)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // UPDATE GROUP PROFILE
    // =====================================================

    public Task<bool> UpdateProfileAsync(
        Guid actor,
        Guid groupId,
        UpdateGroupRequest input,
        CancellationToken ct)
    {
        var name = input.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 100 ||
            input.Description?.Length > 500)
        {
            return Task.FromResult(false);
        }

        return ExecuteAndNotifyAsync(
            async () =>
            {
                // Any active member may update group profile.
                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return false;
                }

                var profile =
                    await repository.GetGroupProfileAsync(
                        groupId, ct);

                if (profile is null)
                    return false;

                // =================================================
                // AVATAR VALIDATION
                // =================================================

                if (input.AvatarFileId is { } fileId &&
                    fileId != profile.AvatarFileId)
                {
                    var file =
                        await repository.GetPendingAvatarAsync(
                            fileId,
                            actor,
                            ct);

                    if (file is null)
                        return false;

                    file.Status = "Avatar";
                    profile.AvatarFileId = fileId;
                }

                // =================================================
                // UPDATE NAME / DESCRIPTION
                // =================================================

                profile.Name = name;
                profile.Description = input.Description?.Trim();

                await repository.SaveChangesAsync(ct);

                return true;
            },
            groupId,
            ct);
    }

    // =====================================================
    // CHANGE GROUP ROLE
    // =====================================================

    public Task<bool> ChangeRoleAsync(
        Guid actor,
        Guid groupId,
        Guid targetId,
        string role,
        CancellationToken ct)
    {
        if (role is not ("Admin" or "Member") ||
            actor == targetId)
        {
            return Task.FromResult(false);
        }

        return ExecuteAndNotifyAsync(
            async () =>
            {
                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return false;
                }

                var actorMember =
                    await ActiveMemberAsync(
                        actor, groupId, ct);

                var target =
                    await ActiveMemberAsync(
                        targetId, groupId, ct);

                if (actorMember is null ||
                    target is null ||
                    target.Role == "Owner")
                {
                    return false;
                }

                // =================================================
                // PROMOTE MEMBER TO ADMIN
                // =================================================

                if (role == "Admin" &&
                    target.Role == "Member" &&
                    actorMember.Role is "Owner" or "Admin")
                {
                    target.Role = "Admin";
                }

                // =================================================
                // DEMOTE ADMIN TO MEMBER
                // =================================================

                else if (role == "Member" &&
                         target.Role == "Admin" &&
                         actorMember.Role == "Owner")
                {
                    target.Role = "Member";
                }
                else
                {
                    return false;
                }

                await repository.SaveChangesAsync(ct);

                return true;
            },
            groupId,
            ct);
    }

    // =====================================================
    // REMOVE GROUP MEMBER
    // =====================================================

    public Task<bool> RemoveAsync(
        Guid actor,
        Guid groupId,
        Guid targetId,
        CancellationToken ct)
    {
        if (actor == targetId)
            return Task.FromResult(false);

        return ExecuteAndNotifyAsync(
            async () =>
            {
                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return false;
                }

                var actorMember =
                    await ActiveMemberAsync(
                        actor, groupId, ct);

                var target =
                    await ActiveMemberAsync(
                        targetId, groupId, ct);

                var conversation =
                    await repository.GetActiveConversationAsync(
                        groupId, ct);

                // =================================================
                // AUTHORIZATION
                // =================================================

                if (actorMember is null ||
                    target is null ||
                    conversation is null ||
                    target.Role == "Owner" ||
                    (
                        actorMember.Role != "Owner" &&
                        !(
                            actorMember.Role == "Admin" &&
                            target.Role == "Member"
                        )
                    ))
                {
                    return false;
                }

                var now = Now;

                // =================================================
                // REMOVE MEMBERSHIP
                // =================================================

                target.LeftAt = now;
                target.LeaveSequence =
                    conversation.LastSequence;
                target.LeftReason = "Removed";

                // =================================================
                // REVOKE INVITE LINKS
                // =================================================

                var links =
                    await repository
                        .GetActiveLinksByMembershipAsync(
                            target.Id, ct);

                foreach (var link in links)
                {
                    link.RevokedAt = now;
                }

                // =================================================
                // REVOKE PENDING INVITATIONS
                // =================================================

                var invitations =
                    await repository
                        .GetPendingInvitationsForUserAsync(
                            groupId,
                            targetId,
                            ct);

                foreach (var invitation in invitations)
                {
                    invitation.Status = "Revoked";
                    invitation.RespondedAt = now;
                }

                await repository.SaveChangesAsync(ct);

                return true;
            },
            groupId,
            ct,
            affectedUserId: targetId);
    }

    // =====================================================
    // TRANSFER GROUP OWNER
    // =====================================================

    public Task<bool> TransferOwnerAsync(
        Guid actor,
        Guid groupId,
        Guid targetId,
        CancellationToken ct)
    {
        if (actor == targetId)
            return Task.FromResult(false);

        return ExecuteAndNotifyAsync(
            async () =>
            {
                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return false;
                }

                var owner =
                    await ActiveMemberAsync(
                        actor, groupId, ct);

                var target =
                    await ActiveMemberAsync(
                        targetId, groupId, ct);

                if (owner?.Role != "Owner" ||
                    target is null)
                {
                    return false;
                }

                // =================================================
                // NEW OWNER ELIGIBILITY
                // =================================================

                var eligible =
                    await repository.IsActiveUserAsync(
                        targetId,
                        Now,
                        ct);

                if (!eligible)
                    return false;

                // =================================================
                // TRANSFER OWNERSHIP
                // =================================================

                owner.Role = "Admin";
                target.Role = "Owner";

                await repository.SaveChangesAsync(ct);

                return true;
            },
            groupId,
            ct);
    }

    // =====================================================
    // DISSOLVE GROUP
    // =====================================================

    public Task<bool> DissolveAsync(
        Guid actor,
        Guid groupId,
        bool confirmed,
        CancellationToken ct)
    {
        if (!confirmed)
            return Task.FromResult(false);

        return ExecuteAndNotifyAsync(
            async () =>
            {
                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return false;
                }

                var member =
                    await ActiveMemberAsync(
                        actor, groupId, ct);

                var conversation =
                    await repository
                        .GetActiveGroupConversationAsync(
                            groupId, ct);

                if (member?.Role != "Owner" ||
                    conversation is null)
                {
                    return false;
                }

                var now = Now;

                // =================================================
                // DISSOLVE CONVERSATION
                // =================================================

                conversation.Status = "Dissolved";
                conversation.UpdatedAt = now;

                // =================================================
                // REVOKE ALL INVITE LINKS
                // =================================================

                var links =
                    await repository.GetActiveGroupLinksAsync(
                        groupId, ct);

                foreach (var link in links)
                {
                    link.RevokedAt = now;
                }

                await repository.SaveChangesAsync(ct);

                return true;
            },
            groupId,
            ct);
    }

    // =====================================================
    // PAGINATED GROUP PINS
    // =====================================================

    public async Task<GroupPinPageDto?> PinPageAsync(
        Guid actor,
        Guid groupId,
        long? before,
        int limit,
        CancellationToken ct)
    {
        // =================================================
        // INITIAL AUTHORIZATION
        // =================================================

        if (!await groups.CanReadAsync(
                actor, groupId, ct))
        {
            return null;
        }

        var pageSize = Math.Clamp(limit, 1, 50);

        // =================================================
        // GET VISIBLE PIN ROWS
        // =================================================

        var rows =
            await repository.GetPinPageRowsAsync(
                actor,
                groupId,
                before,
                pageSize + 1,
                ct);

        // Recheck permission after loading data.
        if (!await groups.CanReadAsync(
                actor, groupId, ct))
        {
            return null;
        }

        // =================================================
        // PAGINATION
        // =================================================

        var hasMore = rows.Count > pageSize;

        var items = rows
            .Take(pageSize)
            .Select(row => new GroupPinDto(
                row.MessageId,
                row.PinnedByUserId,
                row.PinnedAt))
            .ToList();

        long? nextCursor = hasMore
            ? rows[pageSize - 1].Sequence
            : null;

        return new GroupPinPageDto(
            items,
            nextCursor,
            hasMore);
    }

    // =====================================================
    // GET GROUP PINS
    // =====================================================

    public async Task<List<GroupPinDto>?> PinsAsync(
        Guid actor,
        Guid groupId,
        CancellationToken ct)
    {
        if (!await groups.CanReadAsync(
                actor, groupId, ct))
        {
            return null;
        }

        var rows =
            await repository.GetPinRowsAsync(
                groupId, ct);

        var result = new List<GroupPinDto>();

        foreach (var row in rows)
        {
            // =================================================
            // MESSAGE VISIBILITY
            // =================================================

            var canView =
                await groups.CanViewMessageAsync(
                    actor,
                    groupId,
                    row.Sequence,
                    ct);

            if (!canView)
                continue;

            var deleted =
                await repository
                    .IsMessageDeletedForUserAsync(
                        row.MessageId,
                        actor,
                        ct);

            if (deleted)
                continue;

            result.Add(new GroupPinDto(
                row.MessageId,
                row.PinnedByUserId,
                row.PinnedAt));
        }

        return result;
    }

    // =====================================================
    // PIN / UNPIN GROUP MESSAGE
    // =====================================================

    public Task<bool> PinAsync(
        Guid actor,
        Guid groupId,
        Guid messageId,
        bool pin,
        CancellationToken ct)
    {
        return ExecuteAndNotifyAsync(
            async () =>
            {
                // =================================================
                // PERMISSION
                // =================================================

                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return false;
                }

                // =================================================
                // MESSAGE VALIDATION
                // =================================================

                var message =
                    await repository.GetActiveGroupMessageAsync(
                        groupId,
                        messageId,
                        ct);

                if (message is null)
                    return false;

                if (!await groups.CanViewMessageAsync(
                        actor,
                        groupId,
                        message.Sequence,
                        ct))
                {
                    return false;
                }

                var deleted =
                    await repository
                        .IsMessageDeletedForUserAsync(
                            messageId,
                            actor,
                            ct);

                if (deleted)
                    return false;

                // =================================================
                // EXISTING PIN
                // =================================================

                var existing =
                    await repository.GetGroupPinAsync(
                        groupId,
                        messageId,
                        ct);

                // =================================================
                // PIN MESSAGE
                // =================================================

                if (pin && existing is null)
                {
                    repository.AddGroupPin(
                        new GroupPin
                        {
                            Id = Guid.NewGuid(),
                            ConversationId = groupId,
                            MessageId = messageId,
                            PinnedByUserId = actor,
                            PinnedAt = Now
                        });
                }

                // =================================================
                // UNPIN MESSAGE
                // =================================================

                else if (!pin && existing is not null)
                {
                    repository.RemoveGroupPin(existing);
                }

                // Preserve original idempotent behavior:
                // pinning an already pinned message or
                // unpinning an absent pin returns success.

                await repository.SaveChangesAsync(ct);

                return true;
            },
            groupId,
            ct);
    }

    // =====================================================
    // ACTIVE MEMBER HELPER
    // =====================================================

    private Task<ConversationMember?> ActiveMemberAsync(
        Guid userId,
        Guid groupId,
        CancellationToken ct)
    {
        return repository.GetActiveMemberAsync(
            userId,
            groupId,
            ct);
    }

    // =====================================================
    // TRANSACTION + REALTIME
    // =====================================================

    private async Task<bool> ExecuteAndNotifyAsync(
        Func<Task<bool>> operation,
        Guid groupId,
        CancellationToken ct,
        Guid? affectedUserId = null)
    {
        // All database mutations are committed or rolled
        // back before the realtime event is published.

        var success =
            await repository.ExecuteAtomicAsync(
                operation,
                ct);

        if (!success)
            return false;

        // No SignalR event is emitted for rolled-back work.
        await events.GroupChangedAsync(
            groupId,
            ct,
            affectedUserId);

        return true;
    }
}

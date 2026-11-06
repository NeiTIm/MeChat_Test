
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using MeChat.Api.Features.Groups.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Realtime;

namespace MeChat.Api.Features.Groups;

public sealed class GroupInviteLinkService(
    IGroupInviteLinkRepository repository,
    GroupService groups,
    TimeProvider clock,
    StateEvents events)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // CREATE GROUP INVITE LINK
    // =====================================================

    public Task<GroupLinkDto?> CreateAsync(
        Guid actor,
        Guid groupId,
        DateTime? expiresAt,
        CancellationToken ct)
    {
        var now = Now;

        // =================================================
        // VALIDATE EXPIRATION
        // =================================================

        if (expiresAt is not null &&
            (
                expiresAt <= now ||
                expiresAt > now.AddDays(30)
            ))
        {
            return Task.FromResult<GroupLinkDto?>(null);
        }

        return ExecuteAndNotifyAsync<GroupLinkDto?>(
            async () =>
            {
                // =================================================
                // AUTHORIZATION
                // =================================================

                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return null;
                }

                var member =
                    await repository.GetActiveMemberAsync(
                        groupId,
                        actor,
                        ct);

                if (member is null)
                    return null;

                // =================================================
                // GENERATE SECURE TOKEN
                // =================================================

                var token = NewToken();

                var createdAt = Now;

                var link = new GroupInviteLink
                {
                    Id = Guid.NewGuid(),
                    ConversationId = groupId,
                    CreatedByMembershipId = member.Id,
                    TokenHash = Hash(token),
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt
                };

                repository.AddLink(link);

                await repository.SaveChangesAsync(ct);

                // Return the raw token only on creation.
                return new GroupLinkDto(
                    link.Id,
                    actor,
                    link.CreatedAt,
                    expiresAt,
                    token);
            },
            groupId,
            ct);
    }

    // =====================================================
    // LIST GROUP INVITE LINKS
    // =====================================================

    public async Task<List<GroupLinkDto>?> ListAsync(
        Guid actor,
        Guid groupId,
        CancellationToken ct)
    {
        // Only active group members may list links.
        if (!await groups.CanSendAsync(
                actor, groupId, ct))
        {
            return null;
        }

        var member =
            await repository.GetActiveMemberAsync(
                groupId,
                actor,
                ct);

        if (member is null)
            return null;

        // Owner/Admin can list all active links.
        // Members can list only their own links.
        var privileged =
            member.Role is "Owner" or "Admin";

        return await repository.GetLinksAsync(
            groupId,
            actor,
            privileged,
            ct);
    }

    // =====================================================
    // REVOKE GROUP INVITE LINK
    // =====================================================

    public Task<bool> RevokeAsync(
        Guid actor,
        Guid groupId,
        Guid linkId,
        CancellationToken ct)
    {
        return ExecuteAndNotifyAsync<bool>(
            async () =>
            {
                // =================================================
                // AUTHORIZATION
                // =================================================

                if (!await groups.CanSendAsync(
                        actor, groupId, ct))
                {
                    return false;
                }

                var member =
                    await repository.GetActiveMemberAsync(
                        groupId,
                        actor,
                        ct);

                if (member is null)
                    return false;

                // =================================================
                // GET ACTIVE LINK
                // =================================================

                var link =
                    await repository.GetActiveLinkAsync(
                        groupId,
                        linkId,
                        ct);

                if (link is null)
                    return false;

                // =================================================
                // GET LINK CREATOR
                // =================================================

                var creatorId =
                    await repository.GetLinkCreatorUserIdAsync(
                        link.CreatedByMembershipId,
                        ct);

                if (creatorId is null)
                    return false;

                // =================================================
                // PERMISSION RULES
                // =================================================

                var privileged =
                    member.Role is "Owner" or "Admin";

                // Member can revoke only their own links.
                if (!privileged && creatorId != actor)
                    return false;

                // =================================================
                // REVOKE
                // =================================================

                link.RevokedAt = Now;

                await repository.SaveChangesAsync(ct);

                return true;
            },
            groupId,
            ct);
    }

    // =====================================================
    // RESOLVE GROUP LINK PREVIEW
    // =====================================================

    public async Task<GroupLinkPreviewDto?> ResolveAsync(
        Guid actor,
        string token,
        CancellationToken ct)
    {
        // Keep the actor parameter for compatibility
        // with existing Controllers.

        if (!ValidToken(token))
            return null;

        var tokenHash = Hash(token);

        return await repository.GetLinkPreviewAsync(
            tokenHash,
            Now,
            ct);
    }

    // =====================================================
    // JOIN GROUP VIA INVITE LINK
    // =====================================================

    public Task<Guid?> JoinAsync(
        Guid actor,
        string token,
        bool confirmed,
        CancellationToken ct)
    {
        // =================================================
        // INPUT VALIDATION
        // =================================================

        if (!confirmed || !ValidToken(token))
        {
            return Task.FromResult<Guid?>(null);
        }

        var tokenHash = Hash(token);

        return JoinAtomicAsync(
            async () =>
            {
                // =================================================
                // VALIDATE LINK
                // =================================================

                var link =
                    await repository.GetValidLinkByHashAsync(
                        tokenHash,
                        Now,
                        ct);

                if (link is null)
                    return null;

                // =================================================
                // LOCK GROUP CONVERSATION
                // =================================================

                var conversation =
                    await repository.GetActiveGroupForUpdateAsync(
                        link.ConversationId,
                        ct);

                if (conversation is null)
                    return null;

                // =================================================
                // CHECK ACTOR STATUS
                // =================================================

                var userActive =
                    await repository.IsActiveUserAsync(
                        actor,
                        Now,
                        ct);

                if (!userActive)
                    return null;

                // =================================================
                // CHECK BLOCK RELATIONSHIP
                // =================================================

                var creatorId =
                    await repository.GetLinkCreatorUserIdAsync(
                        link.CreatedByMembershipId,
                        ct);

                if (creatorId is null)
                    return null;

                var blocked =
                    await repository.IsBlockedBetweenAsync(
                        actor,
                        creatorId.Value,
                        ct);

                if (blocked)
                    return null;

                // =================================================
                // ALREADY A MEMBER
                // =================================================

                var alreadyMember =
                    await repository.HasActiveMemberAsync(
                        conversation.Id,
                        actor,
                        ct);

                if (alreadyMember)
                    return null;

                // =================================================
                // PREVENT REJOIN THROUGH OLD LINK
                // =================================================

                var lastRemoval =
                    await repository.GetLastRemovalAsync(
                        conversation.Id,
                        actor,
                        ct);

                if (lastRemoval is not null &&
                    link.CreatedAt <= lastRemoval)
                {
                    return null;
                }

                // =================================================
                // GROUP MEMBER LIMIT
                // =================================================

                var count =
                    await repository.CountActiveMembersAsync(
                        conversation.Id,
                        ct);

                if (count >= 100)
                    return null;

                // =================================================
                // ADD NEW MEMBER
                // =================================================

                var member = new ConversationMember
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = actor,
                    Role = "Member",
                    JoinSequence =
                        conversation.LastSequence + 1,
                    JoinedAt = Now
                };

                repository.AddMember(member);

                await repository.SaveChangesAsync(ct);

                return conversation.Id;
            },
            actor,
            ct,
            enableRetry: true);
    }

    // =====================================================
    // GENERATE SECURE RANDOM TOKEN
    // =====================================================

    private static string NewToken()
    {
        return WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));
    }

    // =====================================================
    // HASH INVITE TOKEN
    // =====================================================

    private static string Hash(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token)));
    }

    // =====================================================
    // VALIDATE TOKEN FORMAT
    // =====================================================

    private static bool ValidToken(string? token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length is >= 40 and <= 128 &&
               token.All(character =>
                   char.IsAsciiLetterOrDigit(character) ||
                   character is '-' or '_');
    }

    // =====================================================
    // TRANSACTION + GROUP CHANGED REALTIME
    // =====================================================

    private async Task<T> ExecuteAndNotifyAsync<T>(
        Func<Task<T>> operation,
        Guid groupId,
        CancellationToken ct)
    {
        var result =
            await repository.ExecuteAtomicAsync(
                operation,
                ct);

        // No notification when the business operation
        // returned null or false.
        if (result is null ||
            result is bool success && !success)
        {
            return result;
        }

        // The transaction has already committed.
        await events.GroupChangedAsync(
            groupId,
            ct);

        return result;
    }

    // =====================================================
    // JOIN TRANSACTION + REALTIME
    // =====================================================

    private async Task<Guid?> JoinAtomicAsync(
        Func<Task<Guid?>> operation,
        Guid actor,
        CancellationToken ct,
        bool enableRetry)
    {
        var groupId =
            await repository.ExecuteAtomicAsync(
                operation,
                ct,
                enableRetry);

        if (groupId is null)
            return null;

        // Publish after commit only.
        await events.GroupChangedAsync(
            groupId.Value,
            ct,
            actor);

        return groupId;
    }
}

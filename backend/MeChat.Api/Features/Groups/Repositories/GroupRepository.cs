
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Groups.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Groups.Repositories;

public sealed class GroupRepository(
    AppDbContext db) : IGroupRepository
{
    // =====================================================
    // TRANSACTION + RETRY
    // =====================================================

    public async Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false)
    {
        var attempts = enableRetry ? 3 : 1;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                await using var tx =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable, ct);

                var result = await operation();

                await tx.CommitAsync(ct);

                return result;
            }
            catch (Exception ex) when (
                enableRetry &&
                attempt < attempts - 1 &&
                IsRetryable(ex))
            {
                db.ChangeTracker.Clear();

                await Task.Delay(
                    30 * (attempt + 1), ct);
            }
        }

        throw new InvalidOperationException(
            "Group operation retry limit reached.");
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);

    private static bool IsRetryable(Exception error)
    {
        for (Exception? current = error;
             current is not null;
             current = current.InnerException)
        {
            if (current is SqlException sql &&
                sql.Number is 1205 or 2601 or 2627)
                return true;
        }

        return false;
    }

    // =====================================================
    // USERS / PRIVACY
    // =====================================================

    public async Task<bool> IsActiveUserAsync(
        Guid userId, DateTime now, CancellationToken ct)
    {
        var active = await db.Users.AnyAsync(
            u => u.Id == userId &&
                 u.EmailVerifiedAt != null &&
                 u.DeactivatedAt == null, ct);

        if (!active) return false;

        return !await db.AccountSuspensions.AnyAsync(
            s => s.UserId == userId &&
                 s.Status == "Active" &&
                 s.RevokedAt == null &&
                 (s.ExpiresAt == null || s.ExpiresAt > now),
            ct);
    }

    public Task<bool> AllowsGroupInvitesAsync(
        Guid userId, CancellationToken ct) =>
        db.UserPrivacySettings.AnyAsync(
            p => p.UserId == userId && p.AllowGroupInvites, ct);

    public Task<bool> IsBlockedBetweenAsync(
        Guid first, Guid second, CancellationToken ct) =>
        db.UserBlocks.AnyAsync(
            b => (
                (b.BlockerUserId == first &&
                 b.BlockedUserId == second) ||
                (b.BlockerUserId == second &&
                 b.BlockedUserId == first)
            ) && b.UnblockedAt == null, ct);

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    public void AddConversation(Conversation conversation) =>
        db.Conversations.Add(conversation);

    public Task<Conversation?> GetActiveGroupAsync(
        Guid conversationId, CancellationToken ct) =>
        db.Conversations.SingleOrDefaultAsync(
            c => c.Id == conversationId &&
                 c.Type == "Group" &&
                 c.Status == "Active", ct);

    public Task<Conversation?> GetConversationAsync(
        Guid conversationId, CancellationToken ct) =>
        db.Conversations.SingleOrDefaultAsync(
            c => c.Id == conversationId, ct);

    public Task<bool> GroupExistsAsync(
        Guid conversationId, bool allowDissolved,
        CancellationToken ct) =>
        db.Conversations.AnyAsync(
            c => c.Id == conversationId &&
                 c.Type == "Group" &&
                 (c.Status == "Active" ||
                  (allowDissolved && c.Status == "Dissolved")),
            ct);

    public Task<List<GroupDto>> GetGroupsAsync(
        Guid actor, CancellationToken ct)
    {
        return (
            from conversation in db.Conversations.AsNoTracking()
            join profile in db.GroupProfiles.AsNoTracking()
                on conversation.Id equals profile.ConversationId
            where (conversation.Status == "Active" ||
                   conversation.Status == "Dissolved")
               && db.ConversationMembers.Any(m =>
                    m.ConversationId == conversation.Id &&
                    m.UserId == actor)
               && !db.ConversationUserStates.Any(s =>
                    s.ConversationId == conversation.Id &&
                    s.UserId == actor &&
                    s.IsHidden &&
                    !db.Messages.Any(message =>
                        message.ConversationId == conversation.Id &&
                        message.Sequence > s.HiddenThroughSequence &&
                        db.ConversationMembers.Any(m =>
                            m.ConversationId == conversation.Id &&
                            m.UserId == actor &&
                            message.Sequence >= m.JoinSequence &&
                            (m.LeaveSequence == null ||
                             message.Sequence <= m.LeaveSequence))))
            orderby conversation.UpdatedAt descending
            select new GroupDto(
                conversation.Id,
                profile.Name,
                profile.Description,
                conversation.Status,
                profile.CreatedByUserId,
                conversation.LastSequence,
                db.ConversationMembers
                    .Where(m =>
                        m.ConversationId == conversation.Id &&
                        m.UserId == actor &&
                        m.LeftAt == null)
                    .Select(m => m.Role)
                    .FirstOrDefault(),
                profile.AvatarFileId)
        ).ToListAsync(ct);
    }

    public Task<GroupDto?> GetGroupAsync(
        Guid actor, Guid conversationId, CancellationToken ct)
    {
        return (
            from conversation in db.Conversations.AsNoTracking()
            join profile in db.GroupProfiles.AsNoTracking()
                on conversation.Id equals profile.ConversationId
            where conversation.Id == conversationId
            select new GroupDto(
                conversation.Id,
                profile.Name,
                profile.Description,
                conversation.Status,
                profile.CreatedByUserId,
                conversation.LastSequence,
                db.ConversationMembers
                    .Where(m =>
                        m.ConversationId == conversationId &&
                        m.UserId == actor &&
                        m.LeftAt == null)
                    .Select(m => m.Role)
                    .FirstOrDefault(),
                profile.AvatarFileId)
        ).SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // GROUP PROFILES
    // =====================================================

    public void AddGroupProfile(GroupProfile profile) =>
        db.GroupProfiles.Add(profile);

    // =====================================================
    // MEMBERS
    // =====================================================

    public void AddMember(ConversationMember member) =>
        db.ConversationMembers.Add(member);

    public Task<ConversationMember?> GetActiveMemberAsync(
        Guid conversationId, Guid userId,
        CancellationToken ct) =>
        db.ConversationMembers.SingleOrDefaultAsync(
            m => m.ConversationId == conversationId &&
                 m.UserId == userId &&
                 m.LeftAt == null, ct);

    public Task<bool> HasMemberAsync(
        Guid conversationId, Guid userId,
        bool activeOnly, CancellationToken ct) =>
        db.ConversationMembers.AnyAsync(
            m => m.ConversationId == conversationId &&
                 m.UserId == userId &&
                 (!activeOnly || m.LeftAt == null), ct);

    public Task<int> CountActiveMembersAsync(
        Guid conversationId, CancellationToken ct) =>
        db.ConversationMembers.CountAsync(
            m => m.ConversationId == conversationId &&
                 m.LeftAt == null, ct);

    public Task<DateTime?> GetLastRemovalAsync(
        Guid conversationId, Guid userId,
        CancellationToken ct) =>
        db.ConversationMembers
            .Where(m =>
                m.ConversationId == conversationId &&
                m.UserId == userId &&
                m.LeftReason == "Removed")
            .MaxAsync(m => (DateTime?)m.LeftAt, ct);

    public Task<List<GroupMemberDto>> GetMembersAsync(
        Guid conversationId, CancellationToken ct) =>
        db.ConversationMembers
            .AsNoTracking()
            .Where(m =>
                m.ConversationId == conversationId &&
                m.LeftAt == null)
            .OrderBy(m => m.JoinedAt)
            .ThenBy(m => m.Id)
            .Join(
                db.Users.AsNoTracking(),
                m => m.UserId,
                u => u.Id,
                (m, u) => new GroupMemberDto(
                    m.UserId,
                    m.Role,
                    m.JoinSequence,
                    m.JoinedAt,
                    u.DisplayName,
                    u.Username))
            .ToListAsync(ct);

    public Task<List<Guid>> GetActiveMemberIdsAsync(
        Guid conversationId, CancellationToken ct) =>
        db.ConversationMembers
            .Where(m =>
                m.ConversationId == conversationId &&
                m.LeftAt == null)
            .Select(m => m.UserId)
            .ToListAsync(ct);

    public Task<bool> CanAccessSequenceAsync(
        Guid actor, Guid conversationId,
        long sequence, CancellationToken ct) =>
        db.ConversationMembers.AnyAsync(
            m => m.ConversationId == conversationId &&
                 m.UserId == actor &&
                 sequence >= m.JoinSequence &&
                 (m.LeaveSequence == null ||
                  sequence <= m.LeaveSequence), ct);

    // =====================================================
    // INVITATIONS
    // =====================================================

    public void AddInvitation(GroupInvitation invitation) =>
        db.GroupInvitations.Add(invitation);

    public Task<bool> HasPendingInvitationAsync(
        Guid conversationId, Guid inviteeId,
        CancellationToken ct) =>
        db.GroupInvitations.AnyAsync(
            i => i.ConversationId == conversationId &&
                 i.InviteeUserId == inviteeId &&
                 i.Status == "Pending", ct);

    public Task<GroupInvitation?> GetPendingInvitationAsync(
        Guid invitationId, Guid actor, CancellationToken ct) =>
        db.GroupInvitations.SingleOrDefaultAsync(
            i => i.Id == invitationId &&
                 i.InviteeUserId == actor &&
                 i.Status == "Pending", ct);

    public Task<List<GroupInvitationDto>> GetInvitationsAsync(
        Guid actor, CancellationToken ct)
    {
        return (
            from invitation in db.GroupInvitations.AsNoTracking()
            join conversation in db.Conversations.AsNoTracking()
                on invitation.ConversationId equals conversation.Id
            join profile in db.GroupProfiles.AsNoTracking()
                on conversation.Id equals profile.ConversationId
            where invitation.InviteeUserId == actor &&
                  invitation.Status == "Pending" &&
                  conversation.Status == "Active"
            orderby invitation.CreatedAt descending
            select new GroupInvitationDto(
                invitation.Id,
                conversation.Id,
                invitation.InviterUserId,
                actor,
                profile.Name,
                invitation.CreatedAt)
        ).ToListAsync(ct);
    }

    // =====================================================
    // CONVERSATION USER STATES
    // =====================================================

    public Task<ConversationUserState?> GetUserStateAsync(
        Guid conversationId, Guid actor,
        CancellationToken ct) =>
        db.ConversationUserStates.SingleOrDefaultAsync(
            s => s.ConversationId == conversationId &&
                 s.UserId == actor, ct);

    public void AddUserState(ConversationUserState state) =>
        db.ConversationUserStates.Add(state);

    public Task<bool> IsSequenceHiddenAsync(
        Guid actor, Guid conversationId,
        long sequence, CancellationToken ct) =>
        db.ConversationUserStates.AnyAsync(
            s => s.ConversationId == conversationId &&
                 s.UserId == actor &&
                 sequence <= s.HiddenThroughSequence, ct);

    public Task<List<Guid>> GetMutedMemberIdsAsync(
        Guid conversationId, DateTime now,
        CancellationToken ct) =>
        db.ConversationUserStates
            .Where(s =>
                s.ConversationId == conversationId &&
                s.MutedUntil > now)
            .Select(s => s.UserId)
            .ToListAsync(ct);

    // =====================================================
    // MESSAGES
    // =====================================================

    public Task<Message?> GetMessageByClientIdAsync(
        Guid senderId, Guid clientMessageId,
        CancellationToken ct) =>
        db.Messages.SingleOrDefaultAsync(
            m => m.SenderUserId == senderId &&
                 m.ClientMessageId == clientMessageId, ct);

    public Task<Message?> GetReplyMessageAsync(
        Guid conversationId, Guid messageId,
        CancellationToken ct) =>
        db.Messages.SingleOrDefaultAsync(
            m => m.Id == messageId &&
                 m.ConversationId == conversationId, ct);

    public Task<bool> IsMessageDeletedForUserAsync(
        Guid messageId, Guid actor,
        CancellationToken ct) =>
        db.MessageUserStates.AnyAsync(
            s => s.MessageId == messageId &&
                 s.UserId == actor, ct);

    public void AddMessage(Message message) =>
        db.Messages.Add(message);

    public Task<List<Message>> GetVisibleMessagesAsync(
        Guid actor, Guid conversationId,
        long? before, int take, CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .Where(message =>
                message.ConversationId == conversationId &&
                (before == null || message.Sequence < before) &&
                !db.ConversationUserStates.Any(s =>
                    s.ConversationId == conversationId &&
                    s.UserId == actor &&
                    message.Sequence <= s.HiddenThroughSequence) &&
                db.ConversationMembers.Any(m =>
                    m.ConversationId == conversationId &&
                    m.UserId == actor &&
                    message.Sequence >= m.JoinSequence &&
                    (m.LeaveSequence == null ||
                     message.Sequence <= m.LeaveSequence)) &&
                !db.MessageUserStates.Any(s =>
                    s.MessageId == message.Id &&
                    s.UserId == actor))
            .OrderByDescending(message => message.Sequence)
            .Take(take)
            .ToListAsync(ct);
    }

    // =====================================================
    // NOTIFICATIONS
    // =====================================================

    public void AddNotification(Notification notification) =>
        db.Notifications.Add(notification);
}

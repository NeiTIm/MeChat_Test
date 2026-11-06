
using Microsoft.EntityFrameworkCore;

using MeChat.Api.Features.Conversations.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Conversations;

public sealed class PrivateChatRepository(
    AppDbContext db) : IPrivateChatRepository
{
    // =====================================================
    // TRANSACTION STATE
    // =====================================================

    public bool HasCurrentTransaction =>
        db.Database.CurrentTransaction is not null;

    // =====================================================
    // ACTIVE USERS
    // =====================================================

    public Task<int> CountActiveUsersAsync(
        Guid userA,
        Guid userB,
        CancellationToken ct)
    {
        return db.Users.CountAsync(
            user =>
                (user.Id == userA || user.Id == userB) &&
                user.EmailVerifiedAt != null &&
                user.DeactivatedAt == null,
            ct);
    }

    // =====================================================
    // ACCOUNT SUSPENSIONS
    // =====================================================

    public Task<bool> HasActiveSuspensionAsync(
        Guid userA,
        Guid userB,
        DateTime now,
        CancellationToken ct)
    {
        return db.AccountSuspensions.AnyAsync(
            suspension =>
                (suspension.UserId == userA ||
                 suspension.UserId == userB) &&
                suspension.Status == "Active" &&
                suspension.RevokedAt == null &&
                (suspension.ExpiresAt == null ||
                 suspension.ExpiresAt > now),
            ct);
    }

    // =====================================================
    // BLOCKING
    // =====================================================

    public Task<bool> IsBlockedAsync(
        Guid userA,
        Guid userB,
        CancellationToken ct)
    {
        return db.UserBlocks.AnyAsync(
            block =>
                (
                    (block.BlockerUserId == userA &&
                     block.BlockedUserId == userB) ||
                    (block.BlockerUserId == userB &&
                     block.BlockedUserId == userA)
                ) &&
                block.UnblockedAt == null,
            ct);
    }

    // =====================================================
    // FRIENDSHIP
    // =====================================================

    public Task<bool> AreFriendsAsync(
        Guid first,
        Guid second,
        CancellationToken ct)
    {
        return db.Friendships.AnyAsync(
            friendship =>
                friendship.UserId1 == first &&
                friendship.UserId2 == second &&
                friendship.RemovedAt == null,
            ct);
    }

    // =====================================================
    // STRANGER MESSAGE PRIVACY
    // =====================================================

    public Task<bool> AllowsStrangerMessagesAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.UserPrivacySettings.AnyAsync(
            privacy =>
                privacy.UserId == userId &&
                privacy.AllowStrangerMessages,
            ct);
    }

    // =====================================================
    // GET PRIVATE CONVERSATION PAIR
    // =====================================================

    public Task<PrivateConversationPair?> GetPairAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        return db.PrivateConversationPairs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                pair => pair.ConversationId == conversationId,
                ct);
    }

    // =====================================================
    // GET PAIR BY USERS
    // =====================================================

    public Task<PrivateConversationPair?> GetPairByUsersAsync(
        Guid first,
        Guid second,
        CancellationToken ct)
    {
        return db.PrivateConversationPairs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                pair =>
                    pair.UserId1 == first &&
                    pair.UserId2 == second,
                ct);
    }

    // =====================================================
    // GET CONVERSATION
    // =====================================================

    public Task<Conversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken ct)
    {
        // Tracking is required for LastSequence
        // and UpdatedAt updates during message sending.

        return db.Conversations.SingleOrDefaultAsync(
            conversation =>
                conversation.Id == conversationId,
            ct);
    }

    // =====================================================
    // LIST PRIVATE CONVERSATIONS
    // =====================================================

    public Task<List<PrivateConversationListRow>>
        ListConversationsAsync(
            Guid actor,
            CancellationToken ct)
    {
        return (
            from pair in db.PrivateConversationPairs
            join conversation in db.Conversations
                on pair.ConversationId equals conversation.Id
            where pair.UserId1 == actor ||
                  pair.UserId2 == actor
            orderby conversation.UpdatedAt descending,
                    conversation.Id
            select new PrivateConversationListRow(
                pair.ConversationId,
                pair.UserId1,
                pair.UserId2,
                conversation.Type,
                conversation.Status,
                conversation.LastSequence,
                conversation.UpdatedAt)
        ).AsNoTracking().ToListAsync(ct);
    }

    // =====================================================
    // ADD CONVERSATION
    // =====================================================

    public void AddConversation(Conversation conversation)
    {
        db.Conversations.Add(conversation);
    }

    // =====================================================
    // ADD PRIVATE PAIR
    // =====================================================

    public void AddPair(PrivateConversationPair pair)
    {
        db.PrivateConversationPairs.Add(pair);
    }

    // =====================================================
    // GET DIRECTIONAL ACCESS
    // =====================================================

    public Task<PrivateChatAccess?> GetAccessAsync(
        Guid conversationId,
        Guid sender,
        Guid receiver,
        CancellationToken ct)
    {
        // Tracking is required for changes to
        // Pending, Accepted and Revoked access.

        return db.PrivateChatAccess.SingleOrDefaultAsync(
            access =>
                access.ConversationId == conversationId &&
                access.SenderUserId == sender &&
                access.ReceiverUserId == receiver,
            ct);
    }

    // =====================================================
    // CHECK ACCEPTED ACCESS
    // =====================================================

    public Task<bool> HasAcceptedAccessAsync(
        Guid conversationId,
        Guid actor,
        Guid other,
        bool eitherDirection,
        CancellationToken ct)
    {
        return db.PrivateChatAccess.AnyAsync(
            access =>
                access.ConversationId == conversationId &&
                access.Status == "Accepted" &&
                (
                    (
                        access.SenderUserId == actor &&
                        access.ReceiverUserId == other
                    ) ||
                    (
                        eitherDirection &&
                        access.SenderUserId == other &&
                        access.ReceiverUserId == actor
                    )
                ),
            ct);
    }

    // =====================================================
    // PENDING REQUEST BY DIRECTION
    // =====================================================

    public Task<bool> HasPendingRequestAsync(
        Guid conversationId,
        Guid sender,
        Guid receiver,
        CancellationToken ct)
    {
        return db.MessageRequests.AnyAsync(
            request =>
                request.ConversationId == conversationId &&
                request.SenderUserId == sender &&
                request.ReceiverUserId == receiver &&
                request.Status == "Pending",
            ct);
    }

    // =====================================================
    // PENDING REQUEST ACCESS FOR ACTOR
    // =====================================================

    public Task<bool> HasPendingRequestForUserAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct)
    {
        return db.MessageRequests.AnyAsync(
            request =>
                request.ConversationId == conversationId &&
                request.Status == "Pending" &&
                (
                    request.SenderUserId == actor ||
                    request.ReceiverUserId == actor
                ),
            ct);
    }

    // =====================================================
    // REJECTED OR BLOCKED REQUEST
    // =====================================================

    public Task<bool> HasRejectedOrBlockedRequestAsync(
        Guid conversationId,
        Guid sender,
        Guid receiver,
        CancellationToken ct)
    {
        return db.MessageRequests.AnyAsync(
            request =>
                request.ConversationId == conversationId &&
                request.SenderUserId == sender &&
                request.ReceiverUserId == receiver &&
                (
                    request.Status == "Rejected" ||
                    request.Status == "Blocked"
                ),
            ct);
    }

    // =====================================================
    // ADD ACCESS
    // =====================================================

    public void AddAccess(PrivateChatAccess access)
    {
        db.PrivateChatAccess.Add(access);
    }

    // =====================================================
    // ADD MESSAGE REQUEST
    // =====================================================

    public void AddMessageRequest(MessageRequest request)
    {
        db.MessageRequests.Add(request);
    }

    // =====================================================
    // GET CONVERSATION USER STATE
    // =====================================================

    public Task<ConversationUserState?> GetUserStateAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        // Tracking is required for hide operations.

        return db.ConversationUserStates.SingleOrDefaultAsync(
            state =>
                state.ConversationId == conversationId &&
                state.UserId == userId,
            ct);
    }

    // =====================================================
    // GET HIDDEN CURSOR
    // =====================================================

    public async Task<long> GetHiddenThroughSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct)
    {
        var value = await db.ConversationUserStates
            .AsNoTracking()
            .Where(state =>
                state.ConversationId == conversationId &&
                state.UserId == userId)
            .Select(state => (long?)state.HiddenThroughSequence)
            .SingleOrDefaultAsync(ct);

        return value ?? 0;
    }

    // =====================================================
    // CHECK HIDDEN CONVERSATION
    // =====================================================

    public Task<bool> IsConversationHiddenAsync(
        Guid conversationId,
        Guid userId,
        long lastSequence,
        CancellationToken ct)
    {
        return db.ConversationUserStates.AnyAsync(
            state =>
                state.ConversationId == conversationId &&
                state.UserId == userId &&
                state.IsHidden &&
                state.HiddenThroughSequence >= lastSequence,
            ct);
    }

    // =====================================================
    // CHECK HIDDEN MESSAGE
    // =====================================================

    public Task<bool> IsMessageHiddenAsync(
        Guid conversationId,
        Guid userId,
        long sequence,
        CancellationToken ct)
    {
        return db.ConversationUserStates.AnyAsync(
            state =>
                state.ConversationId == conversationId &&
                state.UserId == userId &&
                sequence <= state.HiddenThroughSequence,
            ct);
    }

    // =====================================================
    // CHECK MUTED CONVERSATION
    // =====================================================

    public Task<bool> IsMutedAsync(
        Guid conversationId,
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.ConversationUserStates.AnyAsync(
            state =>
                state.ConversationId == conversationId &&
                state.UserId == userId &&
                state.MutedUntil > now,
            ct);
    }

    // =====================================================
    // ADD USER STATE
    // =====================================================

    public void AddUserState(ConversationUserState state)
    {
        db.ConversationUserStates.Add(state);
    }

    // =====================================================
    // GET MESSAGE BY CLIENT ID
    // =====================================================

    public Task<Message?> GetMessageByClientIdAsync(
        Guid actor,
        Guid clientMessageId,
        CancellationToken ct)
    {
        return db.Messages.SingleOrDefaultAsync(
            message =>
                message.SenderUserId == actor &&
                message.ClientMessageId == clientMessageId,
            ct);
    }

    // =====================================================
    // GET MESSAGE BY ID
    // =====================================================

    public Task<Message?> GetMessageAsync(
        Guid messageId,
        Guid conversationId,
        CancellationToken ct)
    {
        return db.Messages.SingleOrDefaultAsync(
            message =>
                message.Id == messageId &&
                message.ConversationId == conversationId,
            ct);
    }

    // =====================================================
    // CHECK DELETE FOR ME
    // =====================================================

    public Task<bool> IsMessageDeletedForUserAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct)
    {
        return db.MessageUserStates.AnyAsync(
            state =>
                state.MessageId == messageId &&
                state.UserId == userId,
            ct);
    }

    // =====================================================
    // GET MESSAGE HISTORY
    // =====================================================

    public Task<List<Message>> GetHistoryAsync(
        Guid conversationId,
        Guid actor,
        long hiddenThroughSequence,
        long? before,
        int take,
        CancellationToken ct)
    {
        return db.Messages
            .AsNoTracking()
            .Where(message =>
                message.ConversationId == conversationId &&
                message.Sequence > hiddenThroughSequence &&
                (before == null || message.Sequence < before) &&
                !db.MessageUserStates.Any(state =>
                    state.MessageId == message.Id &&
                    state.UserId == actor))
            .OrderByDescending(message => message.Sequence)
            .Take(take)
            .ToListAsync(ct);
    }

    // =====================================================
    // ADD MESSAGE
    // =====================================================

    public void AddMessage(Message message)
    {
        db.Messages.Add(message);
    }

    // =====================================================
    // ADD NOTIFICATION
    // =====================================================

    public void AddNotification(Notification notification)
    {
        db.Notifications.Add(notification);
    }
}

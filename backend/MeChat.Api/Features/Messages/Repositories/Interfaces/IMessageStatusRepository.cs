
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Messages.Repositories.Interfaces;

public interface IMessageStatusRepository
{
    // =====================================================
    // TRANSACTION / UNIT OF WORK
    // =====================================================

    Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool> shouldCommit,
        CancellationToken ct,
        bool enableRetry = false);

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // MESSAGES
    // =====================================================

    Task<Message?> GetMessageAsync(
        Guid messageId,
        CancellationToken ct);

    Task<Message?> GetSentMessageAsync(
        Guid senderUserId,
        Guid messageId,
        CancellationToken ct);

    Task<List<Message>> GetMessagesByIdsAsync(
        Guid conversationId,
        Guid[] messageIds,
        CancellationToken ct);

    Task<Message?> GetMessageAtSequenceAsync(
        Guid conversationId,
        long sequence,
        CancellationToken ct);

    Task<bool> IsMessageDeletedForUserAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // DELIVERIES
    // =====================================================

    Task<List<Guid>> GetDeliveredRecipientIdsAsync(
        Guid messageId,
        CancellationToken ct);

    Task<List<Guid>> GetExistingDeliveryMessageIdsAsync(
        Guid recipientUserId,
        Guid[] messageIds,
        CancellationToken ct);

    void AddDelivery(MessageDelivery delivery);

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<bool> IsInactiveGroupAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<long?> GetLastSequenceAsync(
        Guid conversationId,
        CancellationToken ct);

    // =====================================================
    // READ CURSORS
    // =====================================================

    Task<bool> HasReadThroughAsync(
        Guid conversationId,
        Guid userId,
        long sequence,
        CancellationToken ct);

    Task<ConversationReadCursor?> GetReadCursorAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    Task<long> GetReadSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    void AddReadCursor(ConversationReadCursor cursor);

    // =====================================================
    // CONVERSATION USER STATE
    // =====================================================

    Task<long> GetHiddenThroughSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // UNREAD
    // =====================================================

    Task<int> CountUnreadMessagesAsync(
        Guid actor,
        Guid conversationId,
        long lowerSequence,
        bool isPrivate,
        CancellationToken ct);

    // =====================================================
    // RECIPIENTS
    // =====================================================

    Task<Guid?> GetOtherPrivateParticipantAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct);

    Task<List<Guid>> GetOtherActiveGroupMembersAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct);
}

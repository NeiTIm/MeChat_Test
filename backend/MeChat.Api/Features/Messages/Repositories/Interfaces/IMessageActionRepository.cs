
using MeChat.Api.Features.Messages;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Messages.Repositories.Interfaces;

public interface IMessageActionRepository
{
    // =====================================================
    // TRANSACTION / PERSISTENCE
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

    Task<Message?> GetUnrecalledMessageAsync(
        Guid messageId,
        CancellationToken ct);

    Task<Message?> GetMessageByClientIdAsync(
        Guid senderUserId,
        Guid clientMessageId,
        CancellationToken ct);

    Task<bool> IsMessageUnrecalledAsync(
        Guid messageId,
        CancellationToken ct);

    Task<bool> IsMessageRecalledAsync(
        Guid messageId,
        CancellationToken ct);

    Task<int> GetEditVersionCountAsync(
        Guid messageId,
        CancellationToken ct);

    Task<List<EditVersionDto>> GetEditVersionsAsync(
        Guid messageId,
        CancellationToken ct);

    void AddEditHistory(
        MessageEditHistory history);

    void AddMessage(
        Message message);

    Task<List<Message>> SearchMessagesBatchAsync(
        Guid conversationId,
        string query,
        long? beforeSequence,
        int batchSize,
        CancellationToken ct);

    // =====================================================
    // MESSAGE USER STATES
    // =====================================================

    Task<bool> HasMessageUserStateAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct);

    void AddMessageUserState(
        MessageUserState state);

    // =====================================================
    // REACTIONS
    // =====================================================

    Task<List<ReactionCountDto>> GetReactionCountsAsync(
        Guid messageId,
        CancellationToken ct);

    Task<string?> GetMyReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct);

    Task<MessageReaction?> GetReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct);

    void AddReaction(
        MessageReaction reaction);

    void RemoveReaction(
        MessageReaction reaction);

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    Task<Conversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<bool> IsInactiveGroupAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<long> GetHiddenThroughSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // RECIPIENTS
    // =====================================================

    Task<List<Guid>> GetGroupRecipientsAsync(
        Guid conversationId,
        Guid? excludeUserId,
        CancellationToken ct);

    Task<List<Guid>> GetPrivateRecipientsAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<bool> IsConversationMutedAsync(
        Guid conversationId,
        Guid userId,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // ATTACHMENTS
    // =====================================================

    Task<List<MessageAttachment>> GetMessageAttachmentsAsync(
        Guid messageId,
        CancellationToken ct);

    void AddMessageAttachment(
        MessageAttachment attachment);

    // =====================================================
    // NOTIFICATIONS
    // =====================================================

    void AddNotification(
        Notification notification);
}

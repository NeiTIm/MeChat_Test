
using MeChat.Api.Features.Conversations;

namespace MeChat.Api.Features.Conversations.Repositories.Interfaces;

public interface IMessageRequestRepository
{
    // =====================================================
    // TRANSACTION
    // =====================================================

    Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct);

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // MESSAGE REQUEST INBOX
    // =====================================================

    Task<List<MessageRequestDto>> GetPendingInboxAsync(
        Guid receiverUserId,
        CancellationToken ct);

    // =====================================================
    // MESSAGE REQUEST
    // =====================================================

    Task<MessageRequest?> GetPendingRequestForUpdateAsync(
        Guid requestId,
        Guid receiverUserId,
        CancellationToken ct);

    // =====================================================
    // PRIVATE CHAT ACCESS
    // =====================================================

    Task<PrivateChatAccess?> GetPrivateChatAccessAsync(
        Guid conversationId,
        Guid senderUserId,
        Guid receiverUserId,
        CancellationToken ct);

    void AddPrivateChatAccess(
        PrivateChatAccess access);

    // =====================================================
    // CONVERSATION
    // =====================================================

    Task<long?> GetLastSequenceAsync(
        Guid conversationId,
        CancellationToken ct);

    // =====================================================
    // CONVERSATION USER STATE
    // =====================================================

    Task<ConversationUserState?> GetConversationUserStateAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    void AddConversationUserState(
        ConversationUserState state);
}

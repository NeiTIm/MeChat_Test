
namespace MeChat.Api.Features.Conversations.Repositories.Interfaces;

public sealed record PrivateConversationListRow(
    Guid ConversationId,
    Guid UserId1,
    Guid UserId2,
    string Type,
    string Status,
    long LastSequence,
    DateTime UpdatedAt);

public interface IPrivateChatRepository
{
    // =====================================================
    // TRANSACTION STATE
    // =====================================================

    bool HasCurrentTransaction { get; }

    // =====================================================
    // ACCOUNT / BLOCK / FRIENDSHIP
    // =====================================================

    Task<int> CountActiveUsersAsync(
        Guid userA,
        Guid userB,
        CancellationToken ct);

    Task<bool> HasActiveSuspensionAsync(
        Guid userA,
        Guid userB,
        DateTime now,
        CancellationToken ct);

    Task<bool> IsBlockedAsync(
        Guid userA,
        Guid userB,
        CancellationToken ct);

    Task<bool> AreFriendsAsync(
        Guid first,
        Guid second,
        CancellationToken ct);

    Task<bool> AllowsStrangerMessagesAsync(
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // PRIVATE CONVERSATION
    // =====================================================

    Task<PrivateConversationPair?> GetPairAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<PrivateConversationPair?> GetPairByUsersAsync(
        Guid first,
        Guid second,
        CancellationToken ct);

    Task<Conversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken ct);

    Task<List<PrivateConversationListRow>>
        ListConversationsAsync(
            Guid actor,
            CancellationToken ct);

    void AddConversation(Conversation conversation);

    void AddPair(PrivateConversationPair pair);

    // =====================================================
    // PRIVATE ACCESS / MESSAGE REQUEST
    // =====================================================

    Task<PrivateChatAccess?> GetAccessAsync(
        Guid conversationId,
        Guid sender,
        Guid receiver,
        CancellationToken ct);

    Task<bool> HasAcceptedAccessAsync(
        Guid conversationId,
        Guid actor,
        Guid other,
        bool eitherDirection,
        CancellationToken ct);

    Task<bool> HasPendingRequestAsync(
        Guid conversationId,
        Guid sender,
        Guid receiver,
        CancellationToken ct);

    Task<bool> HasPendingRequestForUserAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct);

    Task<bool> HasRejectedOrBlockedRequestAsync(
        Guid conversationId,
        Guid sender,
        Guid receiver,
        CancellationToken ct);

    void AddAccess(PrivateChatAccess access);

    void AddMessageRequest(MessageRequest request);

    // =====================================================
    // CONVERSATION USER STATE
    // =====================================================

    Task<ConversationUserState?> GetUserStateAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    Task<long> GetHiddenThroughSequenceAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct);

    Task<bool> IsConversationHiddenAsync(
        Guid conversationId,
        Guid userId,
        long lastSequence,
        CancellationToken ct);

    Task<bool> IsMessageHiddenAsync(
        Guid conversationId,
        Guid userId,
        long sequence,
        CancellationToken ct);

    Task<bool> IsMutedAsync(
        Guid conversationId,
        Guid userId,
        DateTime now,
        CancellationToken ct);

    void AddUserState(ConversationUserState state);

    // =====================================================
    // MESSAGES
    // =====================================================

    Task<Message?> GetMessageByClientIdAsync(
        Guid actor,
        Guid clientMessageId,
        CancellationToken ct);

    Task<Message?> GetMessageAsync(
        Guid messageId,
        Guid conversationId,
        CancellationToken ct);

    Task<bool> IsMessageDeletedForUserAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct);

    Task<List<Message>> GetHistoryAsync(
        Guid conversationId,
        Guid actor,
        long hiddenThroughSequence,
        long? before,
        int take,
        CancellationToken ct);

    void AddMessage(Message message);

    // =====================================================
    // NOTIFICATION
    // =====================================================

    void AddNotification(Notification notification);
}


using MeChat.Api.Features.Conversations;

namespace MeChat.Api.Features.Groups.Repositories.Interfaces;

public interface IGroupRepository
{
    // =====================================================
    // TRANSACTION
    // =====================================================

    Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false);

    Task<int> SaveChangesAsync(CancellationToken ct);

    // =====================================================
    // USERS / PRIVACY / BLOCKS
    // =====================================================

    Task<bool> IsActiveUserAsync(
        Guid userId, DateTime now, CancellationToken ct);

    Task<bool> AllowsGroupInvitesAsync(
        Guid userId, CancellationToken ct);

    Task<bool> IsBlockedBetweenAsync(
        Guid first, Guid second, CancellationToken ct);

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    void AddConversation(Conversation conversation);

    Task<Conversation?> GetActiveGroupAsync(
        Guid conversationId, CancellationToken ct);

    Task<Conversation?> GetConversationAsync(
        Guid conversationId, CancellationToken ct);

    Task<bool> GroupExistsAsync(
        Guid conversationId, bool allowDissolved,
        CancellationToken ct);

    Task<List<GroupDto>> GetGroupsAsync(
        Guid actor, CancellationToken ct);

    Task<GroupDto?> GetGroupAsync(
        Guid actor, Guid conversationId, CancellationToken ct);

    // =====================================================
    // GROUP PROFILES
    // =====================================================

    void AddGroupProfile(GroupProfile profile);

    // =====================================================
    // MEMBERS
    // =====================================================

    void AddMember(ConversationMember member);

    Task<ConversationMember?> GetActiveMemberAsync(
        Guid conversationId, Guid userId, CancellationToken ct);

    Task<bool> HasMemberAsync(
        Guid conversationId, Guid userId,
        bool activeOnly, CancellationToken ct);

    Task<int> CountActiveMembersAsync(
        Guid conversationId, CancellationToken ct);

    Task<DateTime?> GetLastRemovalAsync(
        Guid conversationId, Guid userId, CancellationToken ct);

    Task<List<GroupMemberDto>> GetMembersAsync(
        Guid conversationId, CancellationToken ct);

    Task<List<Guid>> GetActiveMemberIdsAsync(
        Guid conversationId, CancellationToken ct);

    Task<bool> CanAccessSequenceAsync(
        Guid actor, Guid conversationId, long sequence,
        CancellationToken ct);

    // =====================================================
    // INVITATIONS
    // =====================================================

    void AddInvitation(GroupInvitation invitation);

    Task<bool> HasPendingInvitationAsync(
        Guid conversationId, Guid inviteeId,
        CancellationToken ct);

    Task<GroupInvitation?> GetPendingInvitationAsync(
        Guid invitationId, Guid actor, CancellationToken ct);

    Task<List<GroupInvitationDto>> GetInvitationsAsync(
        Guid actor, CancellationToken ct);

    // =====================================================
    // CONVERSATION USER STATES
    // =====================================================

    Task<ConversationUserState?> GetUserStateAsync(
        Guid conversationId, Guid actor, CancellationToken ct);

    void AddUserState(ConversationUserState state);

    Task<bool> IsSequenceHiddenAsync(
        Guid actor, Guid conversationId, long sequence,
        CancellationToken ct);

    Task<List<Guid>> GetMutedMemberIdsAsync(
        Guid conversationId, DateTime now,
        CancellationToken ct);

    // =====================================================
    // MESSAGES
    // =====================================================

    Task<Message?> GetMessageByClientIdAsync(
        Guid senderId, Guid clientMessageId,
        CancellationToken ct);

    Task<Message?> GetReplyMessageAsync(
        Guid conversationId, Guid messageId,
        CancellationToken ct);

    Task<bool> IsMessageDeletedForUserAsync(
        Guid messageId, Guid actor, CancellationToken ct);

    void AddMessage(Message message);

    Task<List<Message>> GetVisibleMessagesAsync(
        Guid actor, Guid conversationId,
        long? before, int take,
        CancellationToken ct);

    // =====================================================
    // NOTIFICATIONS
    // =====================================================

    void AddNotification(Notification notification);
}

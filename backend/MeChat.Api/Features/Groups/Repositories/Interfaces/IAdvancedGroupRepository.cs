
using MeChat.Api.Features.Groups;

namespace MeChat.Api.Features.Groups.Repositories.Interfaces;

public interface IAdvancedGroupRepository
{
    // =====================================================
    // TRANSACTION / PERSISTENCE
    // =====================================================

    Task<bool> ExecuteAtomicAsync(
        Func<Task<bool>> operation,
        CancellationToken ct);

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // GROUP PROFILE
    // =====================================================

    Task<GroupProfile?> GetGroupProfileAsync(
        Guid groupId,
        CancellationToken ct);

    // =====================================================
    // AVATAR FILE
    // =====================================================

    Task<StoredFile?> GetPendingAvatarAsync(
        Guid fileId,
        Guid uploadedByUserId,
        CancellationToken ct);

    // =====================================================
    // GROUP MEMBERS
    // =====================================================

    Task<ConversationMember?> GetActiveMemberAsync(
        Guid userId,
        Guid groupId,
        CancellationToken ct);

    Task<bool> IsActiveUserAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    Task<Conversation?> GetActiveConversationAsync(
        Guid groupId,
        CancellationToken ct);

    Task<Conversation?> GetActiveGroupConversationAsync(
        Guid groupId,
        CancellationToken ct);

    // =====================================================
    // GROUP INVITE LINKS
    // =====================================================

    Task<List<GroupInviteLink>>
        GetActiveLinksByMembershipAsync(
            Guid membershipId,
            CancellationToken ct);

    Task<List<GroupInviteLink>>
        GetActiveGroupLinksAsync(
            Guid groupId,
            CancellationToken ct);

    // =====================================================
    // GROUP INVITATIONS
    // =====================================================

    Task<List<GroupInvitation>>
        GetPendingInvitationsForUserAsync(
            Guid groupId,
            Guid userId,
            CancellationToken ct);

    // =====================================================
    // GROUP PINS
    // =====================================================

    Task<List<GroupPinRow>> GetPinPageRowsAsync(
        Guid actor,
        Guid groupId,
        long? before,
        int take,
        CancellationToken ct);

    Task<List<GroupPinRow>> GetPinRowsAsync(
        Guid groupId,
        CancellationToken ct);

    Task<GroupPin?> GetGroupPinAsync(
        Guid groupId,
        Guid messageId,
        CancellationToken ct);

    void AddGroupPin(GroupPin pin);

    void RemoveGroupPin(GroupPin pin);

    // =====================================================
    // MESSAGES
    // =====================================================

    Task<Message?> GetActiveGroupMessageAsync(
        Guid groupId,
        Guid messageId,
        CancellationToken ct);

    Task<bool> IsMessageDeletedForUserAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct);
}

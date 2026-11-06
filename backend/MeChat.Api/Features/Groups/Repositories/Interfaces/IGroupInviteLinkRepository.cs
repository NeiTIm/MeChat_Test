
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Groups.Repositories.Interfaces;

public interface IGroupInviteLinkRepository
{
    // =====================================================
    // TRANSACTION / PERSISTENCE
    // =====================================================

    Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct,
        bool enableRetry = false);

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // MEMBERSHIPS
    // =====================================================

    Task<ConversationMember?> GetActiveMemberAsync(
        Guid groupId,
        Guid userId,
        CancellationToken ct);

    Task<bool> HasActiveMemberAsync(
        Guid groupId,
        Guid userId,
        CancellationToken ct);

    Task<int> CountActiveMembersAsync(
        Guid groupId,
        CancellationToken ct);

    Task<DateTime?> GetLastRemovalAsync(
        Guid groupId,
        Guid userId,
        CancellationToken ct);

    Task<Guid?> GetLinkCreatorUserIdAsync(
        Guid membershipId,
        CancellationToken ct);

    void AddMember(ConversationMember member);

    // =====================================================
    // GROUP INVITE LINKS
    // =====================================================

    void AddLink(GroupInviteLink link);

    Task<GroupInviteLink?> GetActiveLinkAsync(
        Guid groupId,
        Guid linkId,
        CancellationToken ct);

    Task<GroupInviteLink?> GetValidLinkByHashAsync(
        string tokenHash,
        DateTime now,
        CancellationToken ct);

    Task<List<GroupLinkDto>> GetLinksAsync(
        Guid groupId,
        Guid actor,
        bool privileged,
        CancellationToken ct);

    Task<GroupLinkPreviewDto?> GetLinkPreviewAsync(
        string tokenHash,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // CONVERSATIONS
    // =====================================================

    Task<Conversation?> GetActiveGroupForUpdateAsync(
        Guid groupId,
        CancellationToken ct);

    // =====================================================
    // USERS / SUSPENSIONS / BLOCKS
    // =====================================================

    Task<bool> IsActiveUserAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    Task<bool> IsBlockedBetweenAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);
}

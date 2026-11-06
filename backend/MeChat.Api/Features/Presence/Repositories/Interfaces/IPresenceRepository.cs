
namespace MeChat.Api.Features.Presence.Repositories.Interfaces;

public interface IPresenceRepository
{
    // =====================================================
    // USER STATUS
    // =====================================================

    Task<bool> IsActiveUserAsync(
        Guid userId,
        CancellationToken ct);

    Task<bool> HasActiveSuspensionAsync(
        Guid firstUserId,
        Guid secondUserId,
        DateTime now,
        CancellationToken ct);

    Task<DateTime?> GetLastSeenAsync(
        Guid userId,
        CancellationToken ct);

    Task UpdateLastSeenAsync(
        Guid userId,
        DateTime lastSeenAt,
        CancellationToken ct);

    // =====================================================
    // PRIVACY
    // =====================================================

    Task<(bool ShowOnline, bool ShowLastSeen)>
        GetPrivacyAsync(
            Guid userId,
            CancellationToken ct);

    // =====================================================
    // FRIENDSHIP / PRIVATE CHAT ACCESS
    // =====================================================

    Task<bool> AreFriendsAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    Task<bool> HasAcceptedPrivateChatAccessAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    Task<bool> IsBlockedBetweenAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken ct);

    // =====================================================
    // SESSION
    // =====================================================

    Task<bool> HasActiveSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct);

    // =====================================================
    // CONVERSATION
    // =====================================================

    Task<string?> GetConversationTypeAsync(
        Guid conversationId,
        CancellationToken ct);

    // =====================================================
    // TYPING RECIPIENTS
    // =====================================================

    Task<List<Guid>> GetGroupRecipientsAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct);

    Task<List<Guid>> GetPrivateRecipientsAsync(
        Guid conversationId,
        Guid actor,
        CancellationToken ct);

    // =====================================================
    // PRESENCE BROADCAST RECIPIENTS
    // =====================================================

    Task<List<Guid>> GetPresenceViewerIdsAsync(
        Guid actor,
        CancellationToken ct);
}

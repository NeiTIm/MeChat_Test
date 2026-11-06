
namespace MeChat.Api.Features.Presence;

// =====================================================
// PRESENCE RESPONSE
// =====================================================

public sealed record PresenceDto(
    Guid UserId,
    bool? Online,
    DateTime? LastSeenAt);

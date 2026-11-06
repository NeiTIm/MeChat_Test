
namespace MeChat.Api.Features.Groups;

// =====================================================
// CREATE GROUP INVITE LINK
// =====================================================

public sealed record CreateGroupLinkRequest(
    DateTime? ExpiresAt);

// =====================================================
// GROUP INVITE LINK RESPONSE
// =====================================================

public sealed record GroupLinkDto(
    Guid Id,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string? Token = null);

// =====================================================
// GROUP LINK TOKEN REQUEST
// =====================================================

public sealed record GroupLinkTokenRequest(
    string Token);

// =====================================================
// JOIN GROUP VIA LINK
// =====================================================

public sealed record JoinGroupLinkRequest(
    string Token,
    bool Confirm);

// =====================================================
// GROUP LINK PREVIEW
// =====================================================

public sealed record GroupLinkPreviewDto(
    Guid GroupId,
    string Name,
    string? Description);

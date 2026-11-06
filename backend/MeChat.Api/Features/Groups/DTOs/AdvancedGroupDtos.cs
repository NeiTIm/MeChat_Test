
namespace MeChat.Api.Features.Groups;

// =====================================================
// UPDATE GROUP
// =====================================================

public sealed record UpdateGroupRequest(
    string Name,
    string? Description,
    Guid? AvatarFileId);

// =====================================================
// CHANGE ROLE
// =====================================================

public sealed record ChangeGroupRoleRequest(
    string Role);

// =====================================================
// TRANSFER OWNER
// =====================================================

public sealed record TransferGroupOwnerRequest(
    Guid NewOwnerUserId);

// =====================================================
// DISSOLVE GROUP
// =====================================================

public sealed record DissolveGroupRequest(
    bool Confirm);

// =====================================================
// GROUP PINS
// =====================================================

public sealed record GroupPinDto(
    Guid MessageId,
    Guid PinnedByUserId,
    DateTime PinnedAt);

public sealed record GroupPinPageDto(
    IReadOnlyList<GroupPinDto> Items,
    long? NextCursor,
    bool HasMore);

// =====================================================
// PIN MESSAGE
// =====================================================

public sealed record PinGroupMessageRequest(
    Guid MessageId);

// =====================================================
// INTERNAL REPOSITORY PROJECTION
// =====================================================

// Not an EF Core entity or database table.
// Used to carry message sequence information
// from Repository to Service.

public sealed record GroupPinRow(
    Guid MessageId,
    long Sequence,
    Guid PinnedByUserId,
    DateTime PinnedAt);

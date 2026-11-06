
namespace MeChat.Api.Features.Moderation;

// =====================================================
// APPEAL LOGIN REQUEST
// =====================================================

public sealed record AppealLoginRequest(
    string Identifier,
    string Password);

// =====================================================
// APPEAL ACCESS TOKEN RESPONSE
// =====================================================

public sealed record AppealAccessDto(
    string AccessToken,
    DateTime ExpiresAt);

// =====================================================
// CREATE APPEAL REQUEST
// =====================================================

public sealed record CreateAppealRequest(
    string Reason);

// =====================================================
// APPEAL RESPONSE
// =====================================================

public sealed record AppealDto(
    Guid Id,
    Guid SuspensionId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewNote,
    Guid? UserId = null,
    string? Username = null);


namespace MeChat.Api.Features.Moderation;

// =====================================================
// ADMIN DASHBOARD
// =====================================================

public sealed record AdminActivityDto(
    DateTime Date,
    int NewUsers,
    int Messages);

public sealed record AdminDashboardDto(
    int Users,
    int Messages,
    int Groups,
    int PendingReports,
    IReadOnlyList<AdminActivityDto> Activity);

// =====================================================
// ADMIN USER
// =====================================================

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string Username,
    string SystemRole,
    bool Suspended,
    DateTime CreatedAt);

// =====================================================
// ADMIN GROUP
// =====================================================

public sealed record AdminGroupDto(
    Guid Id,
    string Name,
    string Status,
    int MemberCount);

// =====================================================
// REPORT
// =====================================================

public sealed record AdminReportDto(
    Guid Id,
    Guid ReporterUserId,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    Guid? ReviewedByAdminId,
    DateTime? ReviewedAt);

// =====================================================
// REPORT EVIDENCE
// =====================================================

public sealed record AdminEvidenceDto(
    Guid Id,
    Guid? SourceMessageId,
    string? EvidenceContent,
    Guid? StoredFileId,
    DateTime CapturedAt,
    DateTime? RetainUntil,
    bool LegalHold);

public sealed record AdminCaseDto(
    AdminReportDto Report,
    IReadOnlyList<AdminEvidenceDto> Evidence);

// =====================================================
// REQUESTS
// =====================================================

public sealed record ResolveReportRequest(
    string Status,
    string Note);

public sealed record AdminReasonRequest(
    string Reason,
    DateTime? ExpiresAt = null);

public sealed record ReviewAppealRequest(
    string Status,
    string Note);

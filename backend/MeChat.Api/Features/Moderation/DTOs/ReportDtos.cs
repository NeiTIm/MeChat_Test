
namespace MeChat.Api.Features.Moderation;

// =====================================================
// CREATE REPORT REQUEST
// =====================================================

public sealed record CreateReportRequest(
    string TargetType,
    Guid TargetId,
    string Reason);

// =====================================================
// REPORT RESPONSE
// =====================================================

public sealed record ReportDto(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

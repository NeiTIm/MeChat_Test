
namespace MeChat.Api.Features.Moderation;

// =====================================================
// RETENTION PREVIEW
// =====================================================

public sealed record RetentionPreviewDto(
    int EvidenceEligible,
    int AuditEligible,
    int EvidenceHeld,
    int AuditHeld);

// =====================================================
// LEGAL HOLD REQUEST
// =====================================================

public sealed record RetentionHoldRequest(
    bool Hold,
    string Reason);

// =====================================================
// RETENTION PLAN ITEM
// =====================================================

public sealed record RetentionPlanItem(
    Guid Id,
    DateTime EligibleSince);

// =====================================================
// RETENTION PLAN RESPONSE
// =====================================================

public sealed record RetentionPlanDto(
    string Kind,
    IReadOnlyList<RetentionPlanItem> Items,
    int? NextOffset,
    bool DryRun = true);

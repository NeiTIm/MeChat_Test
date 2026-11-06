
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation;

public static class RetentionPolicy
{
    // =====================================================
    // EVIDENCE ELIGIBILITY
    // =====================================================

    public static bool EvidenceEligible(
        ReportEvidence evidence,
        DateTime now)
    {
        return !evidence.LegalHold
            && evidence.RetainUntil is { } until
            && until <= now;
    }

    // =====================================================
    // AUDIT ELIGIBILITY
    // =====================================================

    public static bool AuditEligible(
        AdminAuditLog audit,
        DateTime now)
    {
        return !audit.LegalHold
            && audit.CreatedAt.AddMonths(12) <= now;
    }

    // =====================================================
    // EVIDENCE + REPORT ELIGIBILITY
    // =====================================================

    public static bool EvidenceEligible(
        ReportEvidence evidence,
        Report report,
        DateTime now)
    {
        return report.Status is "Resolved" or "Dismissed"
            && report.ReviewedAt is { } resolved
            && resolved.AddDays(90) <= now
            && evidence.RetainUntil >= resolved.AddDays(90)
            && EvidenceEligible(evidence, now);
    }
}

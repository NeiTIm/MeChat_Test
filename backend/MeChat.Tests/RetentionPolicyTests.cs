using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeChat.Tests;

public sealed class RetentionPolicyTests
{
    [Fact]
    public async Task Purge_is_disabled_without_touching_SQL_and_rejects_missing_approval_before_SQL()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(DatabaseTarget.DefaultConnection).Options);
        var disabled = new RetentionMaintenance(db, null!, null!, Options.Create(new RetentionMaintenanceOptions()), TimeProvider.System);
        Assert.Equal(0, await disabled.RunAsync(CancellationToken.None));
        var invalid = new RetentionMaintenance(db, null!, null!, Options.Create(new RetentionMaintenanceOptions { EnablePurge = true }), TimeProvider.System);
        await Assert.ThrowsAsync<InvalidOperationException>(() => invalid.RunAsync(CancellationToken.None));
    }
    [Fact]
    public void Evidence_requires_a_closed_case_and_full_ninety_day_cutoff_while_audit_uses_calendar_months()
    {
        var resolved = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var report = new Report { Status = "Resolved", ReviewedAt = resolved };
        var evidence = new ReportEvidence { RetainUntil = resolved.AddDays(90) };
        Assert.False(RetentionPolicy.EvidenceEligible(evidence, report, resolved.AddDays(90).AddTicks(-1)));
        Assert.True(RetentionPolicy.EvidenceEligible(evidence, report, resolved.AddDays(90)));
        evidence.RetainUntil = resolved.AddDays(89);
        Assert.False(RetentionPolicy.EvidenceEligible(evidence, report, resolved.AddDays(100)));
        evidence.RetainUntil = resolved.AddDays(90); report.Status = "Pending";
        Assert.False(RetentionPolicy.EvidenceEligible(evidence, report, resolved.AddDays(100)));
        var leap = new AdminAuditLog { CreatedAt = new DateTime(2024, 2, 29) };
        Assert.False(RetentionPolicy.AuditEligible(leap, new DateTime(2025, 2, 27, 23, 59, 59)));
        Assert.True(RetentionPolicy.AuditEligible(leap, new DateTime(2025, 2, 28)));
        leap.LegalHold = true;
        Assert.False(RetentionPolicy.AuditEligible(leap, new DateTime(2030, 1, 1)));
    }
    [Fact]
    public void Evidence_ninety_days_and_audit_twelve_months_are_independent_and_hold_aware()
    {
        var now = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);
        var evidence = new ReportEvidence { RetainUntil = now.AddDays(-1) };
        var audit = new AdminAuditLog { CreatedAt = now.AddMonths(-11) };
        Assert.True(RetentionPolicy.EvidenceEligible(evidence, now));
        Assert.False(RetentionPolicy.AuditEligible(audit, now));
        evidence.RetainUntil = now.AddDays(1);
        audit.CreatedAt = now.AddMonths(-12);
        Assert.False(RetentionPolicy.EvidenceEligible(evidence, now));
        Assert.True(RetentionPolicy.AuditEligible(audit, now));
        evidence.LegalHold = true;
        audit.LegalHold = true;
        evidence.RetainUntil = now.AddDays(-100);
        Assert.False(RetentionPolicy.EvidenceEligible(evidence, now));
        Assert.False(RetentionPolicy.AuditEligible(audit, now));
        evidence.LegalHold = false;
        evidence.RetainUntil = null;
        Assert.False(RetentionPolicy.EvidenceEligible(evidence, now));
    }
}

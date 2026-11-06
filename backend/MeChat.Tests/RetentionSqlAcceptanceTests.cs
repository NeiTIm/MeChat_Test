using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Moderation;
using MeChat.Api.Realtime;

namespace MeChat.Tests;

public sealed class RetentionSqlAcceptanceTests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(DatabaseTarget.Validate(DatabaseTarget.DefaultConnection)).Options);
    private static RetentionService Service(AppDbContext db, TimeProvider clock) => new(db, new AdminService(db, clock, null!, null!, new SessionConnections()), clock);
    [Fact]
    public async Task Sql_evidence_and_audit_predicates_match_exact_cutoffs_and_holds_without_deleting()
    {
        var clock = new Clock(); var now = clock.GetUtcNow().UtcDateTime; Guid adminId, eligible, held, pending, shortRetention, auditId;
        await using (var setup = Db())
        {
            adminId = await SeedAdmin(setup, now);
            var report = new Report { Id = Guid.NewGuid(), ReporterUserId = adminId, ReviewedByAdminId = adminId, TargetType = "User", TargetId = adminId, Reason = "Synthetic retention predicates", Status = "Resolved", CreatedAt = now.AddDays(-120), ReviewedAt = now.AddDays(-90) };
            var open = new Report { Id = Guid.NewGuid(), ReporterUserId = adminId, ReviewedByAdminId = adminId, TargetType = "User", TargetId = adminId, Reason = "Synthetic open report", Status = "Pending", CreatedAt = now.AddDays(-120), ReviewedAt = now.AddDays(-90) };
            setup.Reports.AddRange(report, open);
            ReportEvidence Evidence(Guid reportId, bool hold, DateTime until) => new() { Id = Guid.NewGuid(), ReportId = reportId, CapturedAt = now.AddDays(-120), RetainUntil = until, LegalHold = hold };
            var yes = Evidence(report.Id, false, now); var hold = Evidence(report.Id, true, now); var no = Evidence(open.Id, false, now); var early = Evidence(report.Id, false, now.AddTicks(-1));
            setup.ReportEvidences.AddRange(yes, hold, no, early); eligible = yes.Id; held = hold.Id; pending = no.Id; shortRetention = early.Id;
            var audit = new AdminAuditLog { Id = Guid.NewGuid(), AdminUserId = adminId, Action = "SyntheticRetentionBoundary", TargetType = "Test", CreatedAt = now.AddMonths(-12) }; auditId = audit.Id; setup.AdminAuditLogs.Add(audit); await setup.SaveChangesAsync();
        }
        async Task<HashSet<Guid>> Plan(string kind)
        {
            await using var db = Db(); var service = Service(db, clock); var result = new HashSet<Guid>(); int? offset = 0;
            while (offset is not null) { var page = await service.PlanAsync(adminId, kind, offset.Value, 100, default); Assert.NotNull(page); Assert.True(page.DryRun); foreach (var item in page.Items) result.Add(item.Id); offset = page.NextOffset; }
            return result;
        }
        clock.Now = now.AddTicks(-1);
        Assert.DoesNotContain(eligible, await Plan("Evidence")); Assert.DoesNotContain(auditId, await Plan("Audit"));
        clock.Now = now;
        var ids = await Plan("Evidence"); Assert.Contains(eligible, ids); foreach (var id in new[] { held, pending, shortRetention }) Assert.DoesNotContain(id, ids);
        Assert.Contains(auditId, await Plan("Audit"));
        await using (var db = Db()) Assert.True(await Service(db, clock).SetHoldAsync(adminId, "Evidence", eligible, new(true, "Synthetic approved preservation"), default));
        Assert.DoesNotContain(eligible, await Plan("Evidence"));
        await using (var db = Db()) Assert.True(await Service(db, clock).SetHoldAsync(adminId, "Evidence", eligible, new(false, "Synthetic release after review"), default));
        Assert.Contains(eligible, await Plan("Evidence"));
        await using var check = Db(); Assert.Equal(4, await check.ReportEvidences.CountAsync(e => new[] { eligible, held, pending, shortRetention }.Contains(e.Id)));
        Assert.True(await check.AdminAuditLogs.AnyAsync(a => a.Id == auditId));
    }
    [Fact]
    public async Task Concurrent_hold_release_has_committed_audits_and_never_purges_the_record()
    {
        var clock = new Clock(); var now = clock.GetUtcNow().UtcDateTime; Guid actor, evidence;
        await using (var db = Db())
        {
            actor = await SeedAdmin(db, now); var report = new Report { Id = Guid.NewGuid(), ReporterUserId = actor, ReviewedByAdminId = actor, TargetType = "User", TargetId = actor, Status = "Resolved", Reason = "Synthetic hold race", CreatedAt = now.AddDays(-120), ReviewedAt = now.AddDays(-100) };
            var row = new ReportEvidence { Id = Guid.NewGuid(), ReportId = report.Id, RetainUntil = now.AddDays(-10), CapturedAt = now.AddDays(-120) }; evidence = row.Id; db.Reports.Add(report); db.ReportEvidences.Add(row); await db.SaveChangesAsync();
        }
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(async i => { await using var db = Db(); return await Service(db, clock).SetHoldAsync(actor, "Evidence", evidence, new(i % 2 == 0, "Synthetic concurrent preservation action"), default); }));
        Assert.All(results, Assert.True);
        await using var check = Db(); Assert.True(await check.ReportEvidences.AnyAsync(e => e.Id == evidence)); Assert.Equal(6, await check.AdminAuditLogs.CountAsync(a => a.TargetId == evidence));
    }
    private static async Task<Guid> SeedAdmin(AppDbContext db, DateTime now)
    {
        var id = Guid.NewGuid(); var suffix = id.ToString("N"); db.Users.Add(new User { Id = id, Email = $"ret-qa-{suffix}@example.invalid", NormalizedEmail = $"RET-QA-{suffix}@EXAMPLE.INVALID", Username = $"rq{suffix}", NormalizedUsername = $"RQ{suffix}", PasswordHash = "Synthetic-no-login", SystemRole = "Admin", EmailVerifiedAt = now, CreatedAt = now }); await db.SaveChangesAsync(); return id;
    }
    private sealed class Clock : TimeProvider { public DateTime Now = DateTime.UtcNow; public override DateTimeOffset GetUtcNow() => new(Now); }
}

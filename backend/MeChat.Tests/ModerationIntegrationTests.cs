using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Tests;

public sealed class ModerationIntegrationTests
{
    [Fact]
    public async Task Case_evidence_is_scoped_and_audited_while_suspended_owner_can_only_appeal()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var admin = factory.CreateClient();
        var secondAdmin = factory.CreateClient();
        var reporter = factory.CreateClient();
        var target = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var adminId = await RegisterAndLogin(admin, directory);
        var secondAdminId = await RegisterAndLogin(secondAdmin, directory);
        var reporterId = await RegisterAndLogin(reporter, directory);
        var targetId = await RegisterAndLogin(target, directory);
        var targetIdentifier = (await target.GetFromJsonAsync<JsonElement>("/api/users/me")).GetProperty("username").GetString();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(u => u.Id == adminId || u.Id == secondAdminId)
                .ExecuteUpdateAsync(update => update.SetProperty(u => u.SystemRole, "Admin"));
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await reporter.GetAsync("/api/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await target.PostAsJsonAsync("/api/friends/requests", new { targetUserId = reporterId })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reporter.PostAsJsonAsync("/api/friends/requests", new { targetUserId = targetId })).StatusCode);
        var opened = await target.PostAsJsonAsync("/api/conversations/private", new { targetUserId = reporterId });
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        var conversationId = (await opened.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        byte[] imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=");
        using var uploadForm = new MultipartFormDataContent();
        uploadForm.Add(new ByteArrayContent(imageBytes), "file", "report.png");
        uploadForm.Add(new StringContent("Message"), "purpose");
        var upload = await target.PostAsync("/api/files", uploadForm);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var fileId = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var sent = await target.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "report snapshot before recall", attachmentIds = new[] { fileId } });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var messageId = (await sent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/conversations/{conversationId}/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync("/api/reports", new { targetType = "Message", targetId = messageId, reason = "Admin cannot fabricate evidence" })).StatusCode);
        var createdReport = await reporter.PostAsJsonAsync("/api/reports", new { targetType = "Message", targetId = messageId, reason = "Valid evidence snapshot" });
        Assert.Equal(HttpStatusCode.OK, createdReport.StatusCode);
        var reportId = (await createdReport.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await reporter.PostAsJsonAsync("/api/reports", new { targetType = "User", targetId, reason = "Synthetic user report" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reporter.PostAsJsonAsync("/api/reports", new { targetType = "Group", targetId = conversationId, reason = "Wrong target type" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await target.PostAsync($"/api/messages/{messageId}/recall", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reporter.GetAsync($"/api/admin/reports/{reportId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/admin/reports/{reportId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/reports/{reportId}/claim", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondAdmin.GetAsync($"/api/admin/reports/{reportId}")).StatusCode);
        var caseBody = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/reports/{reportId}");
        Assert.Equal("report snapshot before recall", caseBody.GetProperty("evidence").EnumerateArray().Single(e => e.GetProperty("evidenceContent").ValueKind != JsonValueKind.Null).GetProperty("evidenceContent").GetString());
        var fileEvidenceId = caseBody.GetProperty("evidence").EnumerateArray().Single(e => e.GetProperty("storedFileId").ValueKind != JsonValueKind.Null).GetProperty("id").GetGuid();
        var evidencePath = $"/api/admin/reports/{reportId}/evidence/{fileEvidenceId}/file";
        Assert.Equal(HttpStatusCode.NotFound, (await secondAdmin.PutAsJsonAsync($"/api/admin/retention/Evidence/{fileEvidenceId}/hold", new { hold = true, reason = "Unauthorized case hold" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/admin/retention/Evidence/{fileEvidenceId}/hold", new { hold = true, reason = "Synthetic lawful evidence preservation" })).StatusCode);
        var heldCase = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/reports/{reportId}");
        Assert.True(heldCase.GetProperty("evidence").EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == fileEvidenceId).GetProperty("legalHold").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/admin/retention/Evidence/{fileEvidenceId}/hold", new { hold = false, reason = "Synthetic approved preservation release" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondAdmin.GetAsync(evidencePath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reporter.GetAsync(evidencePath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/admin/reports/{reportId}/evidence/{Guid.NewGuid()}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reporter.GetAsync($"/api/files/{fileId}/download")).StatusCode);
        Assert.Equal(imageBytes, await admin.GetByteArrayAsync(evidencePath));
        var group = await target.PostAsJsonAsync("/api/groups", new { name = "Owner suspension group", inviteeUserIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.OK, group.StatusCode);
        var groupId = (await group.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await reporter.PostAsJsonAsync("/api/reports", new { targetType = "Group", targetId = groupId, reason = "Outsider report" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await target.PostAsJsonAsync("/api/reports", new { targetType = "Group", targetId = groupId, reason = "Owner group report" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/users/{targetId}/suspend", new { reason = "Synthetic moderation test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.PostAsJsonAsync("/api/auth/login", new { identifier = targetIdentifier, password = "SyntheticModerationPassword!2026" })).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal("Owner", await db.ConversationMembers.Where(m => m.ConversationId == groupId && m.UserId == targetId && m.LeftAt == null).Select(m => m.Role).SingleAsync());
        }
        var appealLogin = await target.PostAsJsonAsync("/api/appeals/authenticate", new { identifier = targetIdentifier, password = "SyntheticModerationPassword!2026" });
        Assert.Equal(HttpStatusCode.OK, appealLogin.StatusCode);
        var appealToken = (await appealLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        target.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appealToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/users/me")).StatusCode);
        var appeal = await target.PostAsJsonAsync("/api/appeals", new { reason = "Please review my synthetic suspension" });
        Assert.Equal(HttpStatusCode.OK, appeal.StatusCode);
        var appealId = (await appeal.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/appeals/{appealId}/review", new { status = "Accepted", note = "Review accepted; no automatic restore" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/reports/{reportId}/resolve", new { status = "Resolved", note = "Evidence reviewed" })).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var evidence = await db.ReportEvidences.SingleAsync(e => e.ReportId == reportId && e.EvidenceContent != null);
            Assert.InRange(evidence.RetainUntil!.Value, DateTime.UtcNow.AddDays(89), DateTime.UtcNow.AddDays(91));
            Assert.True(await db.AdminAuditLogs.AnyAsync(a => a.AdminUserId == adminId && a.Action == "ReadReportEvidence" && a.TargetId == reportId));
            Assert.True(await db.AdminAuditLogs.AnyAsync(a => a.AdminUserId == adminId && a.Action == "ReadReportEvidenceFile" && a.TargetId == reportId));
            Assert.True(await db.AdminAuditLogs.AnyAsync(a => a.AdminUserId == adminId && a.Action == "SuspendUser" && a.TargetId == targetId));
        }
    }

    [Fact]
    public async Task Concurrent_case_claim_and_suspension_have_one_winner_and_one_audit()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var first = factory.CreateClient();
        var second = factory.CreateClient();
        var reporter = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var firstId = await RegisterAndLogin(first, directory);
        var secondId = await RegisterAndLogin(second, directory);
        var targetId = await RegisterAndLogin(reporter, directory);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(u => u.Id == firstId || u.Id == secondId)
                .ExecuteUpdateAsync(update => update.SetProperty(u => u.SystemRole, "Admin"));
        }
        var reportResponse = await reporter.PostAsJsonAsync("/api/reports", new { targetType = "User", targetId = firstId, reason = "Synthetic concurrent claim" });
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        var reportId = (await reportResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var claims = await Task.WhenAll(first.PostAsync($"/api/admin/reports/{reportId}/claim", null), second.PostAsync($"/api/admin/reports/{reportId}/claim", null));
        Assert.Single(claims, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(claims, r => r.StatusCode == HttpStatusCode.NotFound);
        var suspensions = await Task.WhenAll(
            first.PostAsJsonAsync($"/api/admin/users/{targetId}/suspend", new { reason = "Synthetic race A" }),
            second.PostAsJsonAsync($"/api/admin/users/{targetId}/suspend", new { reason = "Synthetic race B" }));
        Assert.Single(suspensions, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(suspensions, r => r.StatusCode == HttpStatusCode.NotFound);
        await using var verification = factory.Services.CreateAsyncScope();
        var check = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await check.AdminAuditLogs.CountAsync(a => a.Action == "ClaimReport" && a.TargetId == reportId));
        Assert.Equal(1, await check.AccountSuspensions.CountAsync(s => s.UserId == targetId && s.RevokedAt == null && s.Status == "Active"));
        Assert.Equal(1, await check.AdminAuditLogs.CountAsync(a => a.Action == "SuspendUser" && a.TargetId == targetId));
    }

    private static async Task<Guid> RegisterAndLogin(HttpClient client, string directory)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"moderation-{suffix}@example.invalid";
        const string password = "SyntheticModerationPassword!2026";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { email, username = $"m{suffix}", password })).StatusCode);
        var token = Directory.GetFiles(directory, "*.json")
            .Select(path => JsonDocument.Parse(File.ReadAllText(path)))
            .First(document => document.RootElement.GetProperty("email").GetString() == email)
            .RootElement.GetProperty("token").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/verify-email", new { token })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return body.GetProperty("userId").GetGuid();
    }
}

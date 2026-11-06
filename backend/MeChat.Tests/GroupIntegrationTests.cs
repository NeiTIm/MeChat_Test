using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Tests;

public sealed class GroupIntegrationTests
{
    [Fact]
    public async Task Group_roles_removal_transfer_pins_and_dissolve_enforce_current_rights()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var owner = factory.CreateClient();
        var admin = factory.CreateClient();
        var member = factory.CreateClient();
        var outsider = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var ownerId = await RegisterAndLogin(owner, directory);
        var adminId = await RegisterAndLogin(admin, directory);
        var memberId = await RegisterAndLogin(member, directory);
        await RegisterAndLogin(outsider, directory);
        var created = await owner.PostAsJsonAsync("/api/groups", new { name = "Advanced group", inviteeUserIds = new[] { adminId, memberId } });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        foreach (var client in new[] { admin, member })
        {
            var invitation = (await client.GetFromJsonAsync<JsonElement>("/api/group-invitations"))[0].GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/group-invitations/{invitation}/accept", null)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PatchAsJsonAsync($"/api/groups/{id}", new { name = "Intrusion", description = "", avatarFileId = (Guid?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PatchAsJsonAsync($"/api/groups/{id}", new { name = "Renamed by member", description = "Allowed", avatarFileId = (Guid?)null })).StatusCode);
        Assert.Equal("Renamed by member", (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}")).GetProperty("name").GetString());
        byte[] groupImage = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=");
        var avatarForm = new MultipartFormDataContent();
        avatarForm.Add(new ByteArrayContent(groupImage), "file", "group.png");
        avatarForm.Add(new StringContent("Avatar"), "purpose");
        var avatarUpload = await member.PostAsync("/api/files", avatarForm);
        Assert.Equal(HttpStatusCode.OK, avatarUpload.StatusCode);
        var avatarId = (await avatarUpload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PatchAsJsonAsync($"/api/groups/{id}", new { name = "Renamed by member", description = "Allowed", avatarFileId = avatarId })).StatusCode);
        Assert.Equal(avatarId, (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}")).GetProperty("avatarFileId").GetGuid());
        Assert.Equal(groupImage, await owner.GetByteArrayAsync($"/api/files/{avatarId}/preview"));
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/files/{avatarId}/preview")).StatusCode);
        var mediaForm = new MultipartFormDataContent();
        mediaForm.Add(new ByteArrayContent(groupImage), "file", "group-message.png");
        mediaForm.Add(new StringContent("Message"), "purpose");
        var mediaUpload = await member.PostAsync("/api/files", mediaForm);
        Assert.Equal(HttpStatusCode.OK, mediaUpload.StatusCode);
        var mediaFileId = (await mediaUpload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await member.PostAsJsonAsync($"/api/groups/{id}/messages", new { clientMessageId = Guid.NewGuid(), content = (string?)null, attachmentIds = new[] { mediaFileId } })).StatusCode);
        Assert.Equal(groupImage, await owner.GetByteArrayAsync($"/api/files/{mediaFileId}/preview"));
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/files/{mediaFileId}/preview")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.PatchAsJsonAsync($"/api/groups/{id}/members/{adminId}/role", new { role = "Member" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PatchAsJsonAsync($"/api/groups/{id}/members/{adminId}/role", new { role = "Admin" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/groups/{id}/members/{memberId}/role", new { role = "Admin" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PatchAsJsonAsync($"/api/groups/{id}/members/{memberId}/role", new { role = "Member" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PatchAsJsonAsync($"/api/groups/{id}/members/{memberId}/role", new { role = "Member" })).StatusCode);
        var sent = await member.PostAsJsonAsync($"/api/groups/{id}/messages", new { clientMessageId = Guid.NewGuid(), content = "pinnable" });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var messageId = (await sent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsJsonAsync($"/api/groups/{id}/pins", new { messageId })).StatusCode);
        Assert.Single((await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/pins")).EnumerateArray());
        Assert.Single((await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/pins/page?limit=1")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/groups/{id}/pins")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/groups/{id}/pins/{messageId}")).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/pins")).EnumerateArray());
        var ownerLinkResponse = await owner.PostAsJsonAsync($"/api/groups/{id}/invite-links", new { expiresAt = (DateTime?)null });
        var memberLinkResponse = await member.PostAsJsonAsync($"/api/groups/{id}/invite-links", new { expiresAt = (DateTime?)null });
        Assert.Equal(HttpStatusCode.OK, ownerLinkResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, memberLinkResponse.StatusCode);
        var ownerLink = await ownerLinkResponse.Content.ReadFromJsonAsync<JsonElement>();
        var memberLink = await memberLinkResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/invite-links")).GetArrayLength());
        Assert.Equal(2, (await admin.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/invite-links")).GetArrayLength());
        Assert.Single((await member.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/invite-links")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await member.DeleteAsync($"/api/groups/{id}/invite-links/{ownerLink.GetProperty("id").GetGuid()}")).StatusCode);
        var ownerToken = ownerLink.GetProperty("token").GetString();
        Assert.Equal(HttpStatusCode.OK, (await outsider.PostAsJsonAsync("/api/group-invite-links/resolve", new { token = ownerToken })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync("/api/group-invite-links/join", new { token = ownerToken, confirm = false })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await outsider.PatchAsJsonAsync("/api/users/me/privacy", new { showOnline = true, showLastSeen = true, allowStrangerMessages = true, allowGroupInvites = false })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await outsider.PostAsJsonAsync("/api/group-invite-links/join", new { token = ownerToken, confirm = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/files/{mediaFileId}/preview")).StatusCode);
        var outsiderId = (await outsider.GetFromJsonAsync<JsonElement>("/api/users/me")).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/groups/{id}/members/{outsiderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync("/api/group-invite-links/join", new { token = ownerToken, confirm = true })).StatusCode);
        var freshLink = await owner.PostAsJsonAsync($"/api/groups/{id}/invite-links", new { expiresAt = (DateTime?)null });
        Assert.Equal(HttpStatusCode.OK, freshLink.StatusCode);
        var freshToken = (await freshLink.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        Assert.Equal(HttpStatusCode.OK, (await outsider.PostAsJsonAsync("/api/group-invite-links/join", new { token = freshToken, confirm = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/groups/{id}/members/{ownerId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/groups/{id}/members/{memberId}")).StatusCode);
        Assert.Equal(groupImage, await member.GetByteArrayAsync($"/api/files/{mediaFileId}/preview"));
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync("/api/group-invite-links/resolve", new { token = memberLink.GetProperty("token").GetString() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsJsonAsync($"/api/groups/{id}/messages", new { clientMessageId = Guid.NewGuid(), content = "removed" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/groups/{id}/invitations", new { inviteeUserId = memberId })).StatusCode);
        var rejoin = (await member.GetFromJsonAsync<JsonElement>("/api/group-invitations"))[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/group-invitations/{rejoin}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsync($"/api/groups/{id}/leave", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/api/groups/{id}/transfer-owner", new { newOwnerUserId = memberId })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/groups/{id}/transfer-owner", new { newOwnerUserId = adminId })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsync($"/api/groups/{id}/leave", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync($"/api/groups/{id}/dissolve", new { confirm = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/api/groups/{id}/dissolve", new { confirm = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/groups/{id}/dissolve", new { confirm = true })).StatusCode);
        Assert.Equal("Dissolved", (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}")).GetProperty("status").GetString());
        Assert.Contains((await owner.GetFromJsonAsync<JsonElement>("/api/groups")).EnumerateArray(), group => group.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsJsonAsync($"/api/groups/{id}/messages", new { clientMessageId = Guid.NewGuid(), content = "after dissolve" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsync($"/api/messages/{messageId}/recall", null)).StatusCode);
        Assert.Equal(2, (await member.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/messages")).GetProperty("items").GetArrayLength());
        Assert.Equal(groupImage, await member.GetByteArrayAsync($"/api/files/{mediaFileId}/preview"));
    }

    [Theory]
    [InlineData("invitation")]
    [InlineData("link")]
    [InlineData("mixed")]
    public async Task Concurrent_acceptance_never_exceeds_one_hundred_members(string joinMode)
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var owner = factory.CreateClient();
        var candidateA = factory.CreateClient();
        var candidateB = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        await RegisterAndLogin(owner, directory);
        var aId = await RegisterAndLogin(candidateA, directory);
        var bId = await RegisterAndLogin(candidateB, directory);
        var created = await owner.PostAsJsonAsync("/api/groups", new { name = "Capacity test" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var groupId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var now = DateTime.UtcNow;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 98; i++)
            {
                var id = Guid.NewGuid();
                var suffix = id.ToString("N");
                db.Users.Add(new User { Id = id, Email = $"capacity-{suffix}@example.invalid", NormalizedEmail = $"CAPACITY-{suffix}@EXAMPLE.INVALID",
                    Username = $"capacity{suffix}", NormalizedUsername = $"CAPACITY{suffix}", DisplayName = "Synthetic capacity member",
                    PasswordHash = "Integration-only-no-login", EmailVerifiedAt = now, CreatedAt = now });
                db.ConversationMembers.Add(new ConversationMember { Id = Guid.NewGuid(), ConversationId = groupId, UserId = id, Role = "Member", JoinSequence = 1, JoinedAt = now });
            }
            await db.SaveChangesAsync();
        }
        Assert.Equal(99, (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{groupId}/members")).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/groups/{groupId}/invitations", new { inviteeUserId = aId })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/groups/{groupId}/invitations", new { inviteeUserId = bId })).StatusCode);
        var aInvite = (await candidateA.GetFromJsonAsync<JsonElement>("/api/group-invitations"))[0].GetProperty("id").GetGuid();
        var bInvite = (await candidateB.GetFromJsonAsync<JsonElement>("/api/group-invitations"))[0].GetProperty("id").GetGuid();
        var link = await owner.PostAsJsonAsync($"/api/groups/{groupId}/invite-links", new { expiresAt = (DateTime?)null });
        Assert.Equal(HttpStatusCode.OK, link.StatusCode);
        var token = (await link.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        var outcomes = await Task.WhenAll(
            joinMode == "invitation" ? candidateA.PostAsync($"/api/group-invitations/{aInvite}/accept", null)
                : candidateA.PostAsJsonAsync("/api/group-invite-links/join", new { token, confirm = true }),
            joinMode == "link" ? candidateB.PostAsJsonAsync("/api/group-invite-links/join", new { token, confirm = true })
                : candidateB.PostAsync($"/api/group-invitations/{bInvite}/accept", null));
        Assert.Equal(1, outcomes.Count(response => response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK));
        Assert.Equal(1, outcomes.Count(response => response.StatusCode == HttpStatusCode.NotFound));
        Assert.Equal(100, (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{groupId}/members")).GetArrayLength());
    }

    [Fact]
    public async Task Group_invitation_requires_consent_and_history_respects_membership_intervals()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var owner = factory.CreateClient();
        var member = factory.CreateClient();
        var outsider = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var ownerId = await RegisterAndLogin(owner, directory);
        var memberId = await RegisterAndLogin(member, directory);
        await RegisterAndLogin(outsider, directory);

        var created = await owner.PostAsJsonAsync("/api/groups", new { name = "Integration group", description = "History boundary", inviteeUserIds = new[] { memberId } });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var groupId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/groups/{groupId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsJsonAsync($"/api/groups/{groupId}/messages", new { clientMessageId = Guid.NewGuid(), content = "before consent" })).StatusCode);
        var invite = (await member.GetFromJsonAsync<JsonElement>("/api/group-invitations"))[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsync($"/api/group-invitations/{invite}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/group-invitations/{invite}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsync($"/api/group-invitations/{invite}/accept", null)).StatusCode);
        Assert.Equal(2, (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{groupId}/members")).GetArrayLength());

        var first = await member.PostAsJsonAsync($"/api/groups/{groupId}/messages", new { clientMessageId = Guid.NewGuid(), content = "first interval" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, firstBody.GetProperty("sequence").GetInt64());
        var firstIdForDelivery = firstBody.GetProperty("id").GetGuid();
        Assert.Equal(1, (await owner.GetFromJsonAsync<JsonElement>($"/api/conversations/{groupId}/unread")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsJsonAsync($"/api/conversations/{groupId}/delivery-acks", new { messageIds = new[] { firstIdForDelivery } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/conversations/{groupId}/delivery-acks", new { messageIds = new[] { firstIdForDelivery } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsJsonAsync($"/api/conversations/{groupId}/read-cursor", new { lastReadSequence = 1 })).StatusCode);
        Assert.Equal(0, (await owner.GetFromJsonAsync<JsonElement>($"/api/conversations/{groupId}/unread")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/conversations/{groupId}/unread")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsync($"/api/groups/{groupId}/leave", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/groups/{groupId}/leave", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.PostAsJsonAsync($"/api/groups/{groupId}/messages", new { clientMessageId = Guid.NewGuid(), content = "after leave" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/groups/{groupId}/messages", new { clientMessageId = Guid.NewGuid(), content = "gap" })).StatusCode);
        var beforeRejoin = await member.GetFromJsonAsync<JsonElement>($"/api/groups/{groupId}/messages");
        Assert.Single(beforeRejoin.GetProperty("items").EnumerateArray());
        Assert.Equal("first interval", beforeRejoin.GetProperty("items")[0].GetProperty("content").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/conversations/{groupId}/unread")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/groups/{groupId}/invitations", new { inviteeUserId = memberId })).StatusCode);
        var secondInvite = (await member.GetFromJsonAsync<JsonElement>("/api/group-invitations"))[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/group-invitations/{secondInvite}/accept", null)).StatusCode);
        var second = await owner.PostAsJsonAsync($"/api/groups/{groupId}/messages", new { clientMessageId = Guid.NewGuid(), content = "second interval" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>();
        var secondId = secondBody.GetProperty("id").GetGuid();
        var history = await member.GetFromJsonAsync<JsonElement>($"/api/groups/{groupId}/messages");
        Assert.Equal(2, history.GetProperty("items").GetArrayLength());
        Assert.DoesNotContain(history.GetProperty("items").EnumerateArray(), item => item.GetProperty("content").GetString() == "gap");
        Assert.Equal(1, (await member.GetFromJsonAsync<JsonElement>($"/api/conversations/{groupId}/unread")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsJsonAsync($"/api/conversations/{groupId}/delivery-acks", new { messageIds = new[] { secondId } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync("/api/blocks", new { targetUserId = memberId })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PutAsJsonAsync($"/api/messages/{secondId}/reaction", new { emoji = "👍" })).StatusCode);
        var reactionRace = await Task.WhenAll(
            member.PutAsJsonAsync($"/api/messages/{secondId}/reaction", new { emoji = "👍" }),
            member.PutAsJsonAsync($"/api/messages/{secondId}/reaction", new { emoji = "❤️" }));
        Assert.All(reactionRace, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.MessageReactions.CountAsync(r => r.MessageId == secondId && r.UserId == memberId));
        }
        Assert.Equal(HttpStatusCode.OK, (await member.PostAsJsonAsync($"/api/groups/{groupId}/messages", new { clientMessageId = Guid.NewGuid(), content = "blocked pair group reply", replyToMessageId = secondId })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/groups/{groupId}/leave", null)).StatusCode);
        var firstId = firstBody.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/messages/{firstId}/recall", null)).StatusCode);
        var recalledHistory = await member.GetFromJsonAsync<JsonElement>($"/api/groups/{groupId}/messages");
        Assert.Equal(JsonValueKind.Null, recalledHistory.GetProperty("items")[2].GetProperty("content").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/groups/{groupId}/messages")).StatusCode);
        Assert.Equal(ownerId, (await member.GetFromJsonAsync<JsonElement>($"/api/groups/{groupId}")).GetProperty("createdByUserId").GetGuid());
    }

    [Fact]
    public async Task Group_hide_protects_history_context_file_pins_and_unread_after_resurfacing()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var owner = factory.CreateClient(); var member = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        await RegisterAndLogin(owner, directory); var memberId = await RegisterAndLogin(member, directory);
        var created = await owner.PostAsJsonAsync("/api/groups", new { name = "Hidden group regression", inviteeUserIds = new[] { memberId } });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var invite = (await member.GetFromJsonAsync<JsonElement>("/api/group-invitations"))[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/group-invitations/{invite}/accept", null)).StatusCode);
        using var form = new MultipartFormDataContent(); form.Add(new StringContent("Message"), "purpose");
        form.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("synthetic hidden attachment")), "file", "hidden.txt");
        var upload = await owner.PostAsync("/api/files", form);
        var fileId = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var sent = await owner.PostAsJsonAsync($"/api/groups/{id}/messages", new { clientMessageId = Guid.NewGuid(), content = "hidden history", attachmentIds = new[] { fileId } });
        var messageId = (await sent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsJsonAsync($"/api/groups/{id}/pins", new { messageId })).StatusCode);
        Assert.Equal(1, (await member.GetFromJsonAsync<JsonElement>("/api/unread")).GetProperty("mainCount").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await member.DeleteAsync($"/api/groups/{id}/for-me")).StatusCode);
        Assert.Empty((await member.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/messages")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/messages/{messageId}/context")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/files/{fileId}/download")).StatusCode);
        Assert.Empty((await member.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/pins/page")).GetProperty("items").EnumerateArray());
        Assert.Equal(0, (await member.GetFromJsonAsync<JsonElement>("/api/unread")).GetProperty("mainCount").GetInt32());
        Assert.Single((await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/messages")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/files/{fileId}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/groups/{id}/messages", new { clientMessageId = Guid.NewGuid(), content = "authorized new message" })).StatusCode);
        Assert.Contains((await member.GetFromJsonAsync<JsonElement>("/api/groups")).EnumerateArray(), row => row.GetProperty("id").GetGuid() == id);
        var history = (await member.GetFromJsonAsync<JsonElement>($"/api/groups/{id}/messages")).GetProperty("items");
        Assert.Single(history.EnumerateArray());
        Assert.Equal("authorized new message", history[0].GetProperty("content").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/files/{fileId}/download")).StatusCode);
    }

    private static async Task<Guid> RegisterAndLogin(HttpClient client, string directory)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"group-{suffix}@example.invalid";
        const string password = "SyntheticGroupPassword!2026";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { email, username = $"g{suffix}", password })).StatusCode);
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

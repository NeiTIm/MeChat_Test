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

public sealed class PrivateChatIntegrationTests
{
    [Fact]
    public async Task Friend_chat_is_durable_idempotent_and_hidden_from_outsiders()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var a = factory.CreateClient();
        var b = factory.CreateClient();
        var outsider = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var first = await RegisterAndLogin(a, directory);
        var second = await RegisterAndLogin(b, directory);
        await RegisterAndLogin(outsider, directory);
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await b.PostAsJsonAsync("/api/friends/requests", new { targetUserId = first })).StatusCode);

        var opened = await a.PostAsJsonAsync("/api/conversations/private", new { targetUserId = second });
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        var conversationId = (await opened.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var secondOpen = await b.PostAsJsonAsync("/api/conversations/private", new { targetUserId = first });
        Assert.Equal(conversationId, (await secondOpen.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        var clientMessageId = Guid.NewGuid();
        var firstSend = await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId, content = "hello from SQL test" });
        Assert.Equal(HttpStatusCode.OK, firstSend.StatusCode);
        var firstMessage = await firstSend.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, firstMessage.GetProperty("sequence").GetInt64());
        var retry = await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId, content = "different retry payload" });
        Assert.Equal(firstMessage.GetProperty("id").GetGuid(), (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        var simultaneousId = Guid.NewGuid();
        var duplicates = await Task.WhenAll(
            a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = simultaneousId, content = "parallel retry" }),
            a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = simultaneousId, content = "parallel retry" }));
        Assert.All(duplicates, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var duplicateMessages = await Task.WhenAll(duplicates.Select(response => response.Content.ReadFromJsonAsync<JsonElement>()));
        Assert.Equal(duplicateMessages[0].GetProperty("id").GetGuid(), duplicateMessages[1].GetProperty("id").GetGuid());
        Assert.Equal(2, duplicateMessages[0].GetProperty("sequence").GetInt64());

        var history = await (await b.GetAsync($"/api/conversations/{conversationId}/messages")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, history.GetProperty("items").GetArrayLength());
        Assert.Equal("hello from SQL test", history.GetProperty("items")[1].GetProperty("content").GetString());
        Assert.Equal(2, (await b.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/unread")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/conversations/{conversationId}/delivery-acks", new { messageIds = new[] { firstMessage.GetProperty("id").GetGuid() } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync($"/api/conversations/{conversationId}/delivery-acks", new { messageIds = new[] { firstMessage.GetProperty("id").GetGuid() } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync($"/api/conversations/{conversationId}/delivery-acks", new { messageIds = new[] { firstMessage.GetProperty("id").GetGuid() } })).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.MessageDeliveries.CountAsync(d => d.MessageId == firstMessage.GetProperty("id").GetGuid() && d.RecipientUserId == second));
        }
        var bDevice2 = factory.CreateClient();
        var bEmail = (await b.GetFromJsonAsync<JsonElement>("/api/users/me")).GetProperty("email").GetString();
        var bLogin = await bDevice2.PostAsJsonAsync("/api/auth/login", new { identifier = bEmail, password = "SyntheticChatPassword!2026" });
        Assert.Equal(HttpStatusCode.OK, bLogin.StatusCode);
        bDevice2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await bLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var readRace = await Task.WhenAll(
            b.PutAsJsonAsync($"/api/conversations/{conversationId}/read-cursor", new { lastReadSequence = 2 }),
            bDevice2.PutAsJsonAsync($"/api/conversations/{conversationId}/read-cursor", new { lastReadSequence = 1 }));
        Assert.All(readRace, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
        Assert.Equal(HttpStatusCode.NotFound, (await b.PutAsJsonAsync($"/api/conversations/{conversationId}/read-cursor", new { lastReadSequence = 999 })).StatusCode);
        var unread = await b.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/unread");
        Assert.Equal(0, unread.GetProperty("count").GetInt32());
        var receipt = await a.GetFromJsonAsync<JsonElement>($"/api/messages/{firstMessage.GetProperty("id").GetGuid()}/receipts");
        Assert.Equal("Seen", receipt.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/messages/{firstMessage.GetProperty("id").GetGuid()}/receipts")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/messages/{firstMessage.GetProperty("id").GetGuid()}/receipts")).StatusCode);
        Assert.Equal(2, unread.GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(2, (await bDevice2.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/unread")).GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/conversations/{conversationId}/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/conversations/{conversationId}/unread")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "intrusion" })).StatusCode);

        var firstId = firstMessage.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await b.PatchAsJsonAsync($"/api/messages/{firstId}", new { content = "intruder edit" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.PatchAsJsonAsync($"/api/messages/{firstId}", new { content = "edited text" })).StatusCode);
        var edits = await b.GetFromJsonAsync<JsonElement>($"/api/messages/{firstId}/edit-history");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/messages/{firstId}/context")).StatusCode);
        Assert.Equal(2, edits.GetProperty("versions").GetArrayLength());
        Assert.Equal("hello from SQL test", edits.GetProperty("versions")[0].GetProperty("content").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await b.PutAsJsonAsync($"/api/messages/{firstId}/reaction", new { emoji = "👍" })).StatusCode);
        Assert.Single((await b.GetFromJsonAsync<JsonElement>($"/api/messages/{firstId}/context")).GetProperty("reactions").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await b.DeleteAsync($"/api/messages/{firstId}/reaction")).StatusCode);
        Assert.Empty((await a.GetFromJsonAsync<JsonElement>($"/api/messages/{firstId}/context")).GetProperty("reactions").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await b.DeleteAsync($"/api/messages/{firstId}/for-me")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/messages/{firstId}/context")).StatusCode);
        Assert.Single((await b.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/messages")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsync($"/api/messages/{firstId}/recall", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/messages/{firstId}/edit-history")).StatusCode);
        Assert.Equal(JsonValueKind.Null, (await a.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/messages")).GetProperty("items")[1].GetProperty("content").ValueKind);
        var reply = await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "reply to tombstone", replyToMessageId = firstId });
        Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
        var replyBody = await reply.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstId, replyBody.GetProperty("replyToMessageId").GetGuid());
        var replyContext = await a.GetFromJsonAsync<JsonElement>($"/api/messages/{replyBody.GetProperty("id").GetGuid()}/context");
        Assert.False(replyContext.GetProperty("reply").GetProperty("available").GetBoolean());
        Assert.Equal(JsonValueKind.Null, replyContext.GetProperty("reply").GetProperty("content").ValueKind);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Messages.Where(m => m.SenderUserId == first && m.ClientMessageId == simultaneousId)
                .ExecuteUpdateAsync(update => update.SetProperty(m => m.CreatedAt, DateTime.UtcNow.AddMinutes(-16)));
        }
        var secondId = duplicateMessages[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await a.PatchAsJsonAsync($"/api/messages/{secondId}", new { content = "too late" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync($"/api/conversations/{conversationId}/for-me")).StatusCode);
        Assert.Empty((await a.GetFromJsonAsync<JsonElement>("/api/conversations")).EnumerateArray());
        Assert.Empty((await a.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/messages")).GetProperty("items").EnumerateArray());
        var resurfaced = await b.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "resurface" });
        Assert.Equal(HttpStatusCode.OK, resurfaced.StatusCode);
        Assert.Single((await a.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/messages")).GetProperty("items").EnumerateArray());
        Assert.Single((await a.GetFromJsonAsync<JsonElement>("/api/conversations")).EnumerateArray());
        var sourceId = (await resurfaced.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var forward = await b.PostAsJsonAsync($"/api/messages/{sourceId}/forward", new { destinationConversationId = conversationId, clientMessageId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, forward.StatusCode);
        var forwardedId = (await forward.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsync($"/api/messages/{sourceId}/recall", null)).StatusCode);
        var afterForward = await a.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/messages");
        Assert.Equal("resurface", afterForward.GetProperty("items")[0].GetProperty("content").GetString());
        Assert.Equal(forwardedId, afterForward.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.True(afterForward.GetProperty("items")[0].GetProperty("isForwarded").GetBoolean());
        var search = await a.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/messages/search?q=resurface");
        Assert.Single(search.GetProperty("items").EnumerateArray());
        Assert.Equal(forwardedId, search.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/conversations/{conversationId}/messages/search?q=resurface")).StatusCode);
        static MultipartFormDataContent UploadBody(string fileName, byte[] bytes)
        {
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes), "file", fileName);
            form.Add(new StringContent("Message"), "purpose");
            return form;
        }
        var fakeImage = await a.PostAsync("/api/files", UploadBody("fake.png", [1, 2, 3, 4]));
        Assert.Equal(HttpStatusCode.BadRequest, fakeImage.StatusCode);
        byte[] imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=");
        var upload = await a.PostAsync("/api/files", UploadBody("proof.png", imageBytes));
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var fileId = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/files/{fileId}/preview")).StatusCode);
        Assert.Equal(imageBytes, await a.GetByteArrayAsync($"/api/files/{fileId}/preview"));
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "stolen file", attachmentIds = new[] { fileId } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "duplicate file", attachmentIds = new[] { fileId, fileId } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "too many", attachmentIds = Enumerable.Repeat(fileId, 6).ToArray() })).StatusCode);
        var mediaSend = await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = (string?)null, attachmentIds = new[] { fileId } });
        Assert.Equal(HttpStatusCode.OK, mediaSend.StatusCode);
        var mediaBody = await mediaSend.Content.ReadFromJsonAsync<JsonElement>();
        var mediaId = mediaBody.GetProperty("id").GetGuid();
        Assert.Single(mediaBody.GetProperty("attachments").EnumerateArray());
        Assert.Equal(imageBytes, await b.GetByteArrayAsync($"/api/files/{fileId}/download"));
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/files/{fileId}/download")).StatusCode);
        var mediaForward = await b.PostAsJsonAsync($"/api/messages/{mediaId}/forward", new { destinationConversationId = conversationId, clientMessageId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, mediaForward.StatusCode);
        var mediaForwardId = (await mediaForward.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsync($"/api/messages/{mediaId}/recall", null)).StatusCode);
        Assert.Equal(imageBytes, await b.GetByteArrayAsync($"/api/files/{fileId}/preview"));
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync($"/api/messages/{mediaForwardId}/for-me")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/files/{fileId}/preview")).StatusCode);
        Assert.Equal(imageBytes, await b.GetByteArrayAsync($"/api/files/{fileId}/preview"));
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsync($"/api/messages/{mediaForwardId}/recall", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/files/{fileId}/preview")).StatusCode);
        var avatarForm = new MultipartFormDataContent();
        avatarForm.Add(new ByteArrayContent(imageBytes), "file", "avatar.png");
        avatarForm.Add(new StringContent("Avatar"), "purpose");
        var avatarUpload = await a.PostAsync("/api/files", avatarForm);
        Assert.Equal(HttpStatusCode.OK, avatarUpload.StatusCode);
        var avatarId = (await avatarUpload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync("/api/users/me/avatar", new { storedFileId = avatarId })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsJsonAsync("/api/users/me/avatar", new { storedFileId = avatarId })).StatusCode);
        Assert.Equal(avatarId, (await a.GetFromJsonAsync<JsonElement>("/api/users/me")).GetProperty("avatarFileId").GetGuid());
        Assert.Equal(imageBytes, await b.GetByteArrayAsync($"/api/files/{avatarId}/preview"));
        var notificationBefore = (await b.GetFromJsonAsync<JsonElement>("/api/notifications/unread-count")).GetProperty("count").GetInt32();
        Assert.True(notificationBefore >= 1);
        Assert.Equal(HttpStatusCode.NoContent, (await b.PatchAsJsonAsync($"/api/conversations/{conversationId}/settings", new { mutedUntil = DateTime.UtcNow.AddHours(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/conversations/{conversationId}/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "muted message" })).StatusCode);
        Assert.Equal(notificationBefore, (await b.GetFromJsonAsync<JsonElement>("/api/notifications/unread-count")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await b.PatchAsJsonAsync($"/api/conversations/{conversationId}/settings", new { mutedUntil = (DateTime?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "unmuted message" })).StatusCode);
        Assert.Equal(notificationBefore + 1, (await b.GetFromJsonAsync<JsonElement>("/api/notifications/unread-count")).GetProperty("count").GetInt32());
        var notification = (await b.GetFromJsonAsync<JsonElement>("/api/notifications")).GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PatchAsync($"/api/notifications/{notification}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.PatchAsync($"/api/notifications/{notification}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsync("/api/notifications/read-all", null)).StatusCode);
        Assert.Equal(0, (await b.GetFromJsonAsync<JsonElement>("/api/notifications/unread-count")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/api/users/{second}/presence")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/users/{second}/presence")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync("/api/blocks", new { targetUserId = first })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/users/{second}/presence")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "blocked" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.DeleteAsync($"/api/blocks/{first}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "still revoked" })).StatusCode);
    }

    [Fact]
    public async Task Stranger_request_is_directional_and_does_not_create_friendship()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var sender = factory.CreateClient();
        var receiver = factory.CreateClient();
        var outsider = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var senderId = await RegisterAndLogin(sender, directory);
        var receiverId = await RegisterAndLogin(receiver, directory);
        await RegisterAndLogin(outsider, directory);
        var opened = await sender.PostAsJsonAsync("/api/conversations/private", new { targetUserId = receiverId });
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        var conversationId = (await opened.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.OK, (await sender.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = $"pending {i}" })).StatusCode);
        Assert.Empty((await receiver.GetFromJsonAsync<JsonElement>("/api/conversations")).EnumerateArray());
        var inbox = await receiver.GetFromJsonAsync<JsonElement>("/api/message-requests");
        Assert.Single(inbox.EnumerateArray());
        var requestId = inbox[0].GetProperty("id").GetGuid();
        Assert.Equal(10, inbox[0].GetProperty("lastSequence").GetInt64());
        var aggregateBefore = await receiver.GetFromJsonAsync<JsonElement>("/api/unread");
        Assert.Equal(0, aggregateBefore.GetProperty("mainCount").GetInt32());
        Assert.Equal(1, aggregateBefore.GetProperty("requestCount").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await receiver.PutAsJsonAsync($"/api/conversations/{conversationId}/read-cursor", new { lastReadSequence = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsync($"/api/message-requests/{requestId}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await receiver.PutAsJsonAsync($"/api/message-requests/{requestId}/read-cursor", new { lastReadSequence = 2 })).StatusCode);
        Assert.Equal(1, (await receiver.GetFromJsonAsync<JsonElement>("/api/message-requests/unread-count")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await receiver.PutAsJsonAsync($"/api/message-requests/{requestId}/read-cursor", new { lastReadSequence = 1 })).StatusCode);
        Assert.Equal(2, (await receiver.GetFromJsonAsync<JsonElement>("/api/message-requests"))[0].GetProperty("requestReadSequence").GetInt64());
        var receiverDevice2 = factory.CreateClient();
        var email = (await receiver.GetFromJsonAsync<JsonElement>("/api/users/me")).GetProperty("email").GetString();
        var device2Login = await receiverDevice2.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = "SyntheticChatPassword!2026" });
        Assert.Equal(HttpStatusCode.OK, device2Login.StatusCode);
        receiverDevice2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await device2Login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var cursorRace = await Task.WhenAll(
            receiver.PutAsJsonAsync($"/api/message-requests/{requestId}/read-cursor", new { lastReadSequence = 10 }),
            receiverDevice2.PutAsJsonAsync($"/api/message-requests/{requestId}/read-cursor", new { lastReadSequence = 5 }));
        Assert.All(cursorRace, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
        Assert.Equal(10, (await receiverDevice2.GetFromJsonAsync<JsonElement>("/api/message-requests"))[0].GetProperty("requestReadSequence").GetInt64());
        var pendingHistory = await sender.GetFromJsonAsync<JsonElement>($"/api/conversations/{conversationId}/messages");
        var pendingMessage = pendingHistory.GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await receiver.PostAsJsonAsync($"/api/conversations/{conversationId}/delivery-acks", new { messageIds = new[] { pendingMessage } })).StatusCode);
        var pendingReceipt = await sender.GetFromJsonAsync<JsonElement>($"/api/messages/{pendingMessage}/receipts");
        Assert.Equal(0, pendingReceipt.GetProperty("seenCount").GetInt32());
        Assert.Equal("Delivered", pendingReceipt.GetProperty("status").GetString());
        var aggregateAfter = await receiverDevice2.GetFromJsonAsync<JsonElement>("/api/unread");
        Assert.Equal(0, aggregateAfter.GetProperty("mainCount").GetInt32());
        Assert.Equal(0, aggregateAfter.GetProperty("requestCount").GetInt32());
        Assert.Equal(0, (await receiver.GetFromJsonAsync<JsonElement>("/api/message-requests/unread-count")).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await receiver.PostAsync($"/api/message-requests/{requestId}/reject", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await sender.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "rejected" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await receiver.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "reverse request" })).StatusCode);
        var reverse = await sender.GetFromJsonAsync<JsonElement>("/api/message-requests");
        Assert.Single(reverse.EnumerateArray());
        Assert.Equal(receiverId, reverse[0].GetProperty("senderUserId").GetGuid());
        Assert.Equal(HttpStatusCode.NoContent, (await sender.PostAsync($"/api/message-requests/{reverse[0].GetProperty("id").GetGuid()}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sender.PostAsJsonAsync($"/api/conversations/{conversationId}/messages", new { clientMessageId = Guid.NewGuid(), content = "accepted reply" })).StatusCode);
        Assert.Empty((await sender.GetFromJsonAsync<JsonElement>("/api/friends")).EnumerateArray());
        Assert.Equal(senderId, (await receiver.GetFromJsonAsync<JsonElement>("/api/conversations"))[0].GetProperty("otherUserId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await receiver.PatchAsJsonAsync("/api/users/me/privacy", new { showOnline = true, showLastSeen = true, allowStrangerMessages = false, allowGroupInvites = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await receiver.PatchAsJsonAsync("/api/users/me/privacy", new { showOnline = false, showLastSeen = false, allowStrangerMessages = false, allowGroupInvites = true })).StatusCode);
        var hiddenPresence = await sender.GetFromJsonAsync<JsonElement>($"/api/users/{receiverId}/presence");
        Assert.Equal(JsonValueKind.Null, hiddenPresence.GetProperty("online").ValueKind);
        Assert.Equal(JsonValueKind.Null, hiddenPresence.GetProperty("lastSeenAt").ValueKind);
        var thirdOpen = await outsider.PostAsJsonAsync("/api/conversations/private", new { targetUserId = receiverId });
        Assert.Equal(HttpStatusCode.OK, thirdOpen.StatusCode);
        var thirdId = (await thirdOpen.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync($"/api/conversations/{thirdId}/messages", new { clientMessageId = Guid.NewGuid(), content = "privacy denied" })).StatusCode);
        Assert.Empty((await receiver.GetFromJsonAsync<JsonElement>("/api/message-requests")).EnumerateArray());
    }

    private static async Task<Guid> RegisterAndLogin(HttpClient client, string directory)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"chat-{suffix}@example.invalid";
        var password = "SyntheticChatPassword!2026";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { email, username = $"c{suffix}", password })).StatusCode);
        var token = Directory.GetFiles(directory, "*.json")
            .Select(path => JsonDocument.Parse(File.ReadAllText(path)))
            .First(document => document.RootElement.GetProperty("email").GetString() == email)
            .RootElement.GetProperty("token").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/verify-email", new { token })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password });
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return body.GetProperty("userId").GetGuid();
    }
}

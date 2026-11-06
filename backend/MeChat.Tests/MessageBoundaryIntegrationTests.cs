using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MeChat.Tests;

public sealed class MessageBoundaryIntegrationTests
{
    [Fact]
    public async Task Edit_accepts_exact_fifteen_minutes_changes_text_only_and_recall_has_no_age_limit()
    {
        var clock = new Clock();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => { builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) })); builder.ConfigureServices(services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock); }); });
        using var a = factory.CreateClient(); using var b = factory.CreateClient(); var first = await MediaBoundaryIntegrationTests.Account(factory, a); var second = await MediaBoundaryIntegrationTests.Account(factory, b);
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second })).StatusCode); Assert.Equal(HttpStatusCode.OK, (await b.PostAsJsonAsync("/api/friends/requests", new { targetUserId = first })).StatusCode);
        var conversation = (await (await a.PostAsJsonAsync("/api/conversations/private", new { targetUserId = second })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent([65]), "file", "edit-fixture.txt"); form.Add(new StringContent("Message"), "purpose");
        var upload = await a.PostAsync("/api/files", form); Assert.Equal(HttpStatusCode.OK, upload.StatusCode); var file = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var sent = await a.PostAsJsonAsync($"/api/conversations/{conversation}/messages", new { clientMessageId = Guid.NewGuid(), content = "original", attachmentIds = new[] { file } }); Assert.Equal(HttpStatusCode.OK, sent.StatusCode); var message = (await sent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        clock.Now = clock.Now.AddMinutes(15);
        var edit = await a.PatchAsJsonAsync($"/api/messages/{message}", new { content = "exact-boundary", attachmentIds = new[] { Guid.NewGuid() } }); Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal(file, (await edit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attachments")[0].GetProperty("id").GetGuid());
        clock.Now = clock.Now.AddTicks(1); Assert.Equal(HttpStatusCode.NotFound, (await a.PatchAsJsonAsync($"/api/messages/{message}", new { content = "late" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PatchAsJsonAsync($"/api/messages/{message}", new { content = "not owner" })).StatusCode);
        clock.Now = clock.Now.AddDays(365); Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsync($"/api/messages/{message}/recall", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/messages/{message}/edit-history")).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/files/{file}/download")).StatusCode);
    }
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
}

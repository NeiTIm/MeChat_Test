using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MeChat.Tests;

public sealed class GroupPagingRaceTests
{
    private static WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) })));
    [Fact]
    public async Task Pin_pagination_covers_more_than_thirty_without_duplicates_and_respects_hidden_history()
    {
        await using var factory = Factory(); using var owner = factory.CreateClient(); using var member = factory.CreateClient(); await MediaBoundaryIntegrationTests.Account(factory, owner); var id = await MediaBoundaryIntegrationTests.Account(factory, member);
        var create = await owner.PostAsJsonAsync("/api/groups", new { name = "Synthetic paged pins", inviteeUserIds = new[] { id } }); Assert.Equal(HttpStatusCode.OK, create.StatusCode); var group = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var invites = await member.GetFromJsonAsync<JsonElement>("/api/group-invitations"); Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/api/group-invitations/{invites[0].GetProperty("id").GetGuid()}/accept", null)).StatusCode);
        var expected = new HashSet<Guid>();
        for (var i = 0; i < 35; i++)
        {
            var response = await owner.PostAsJsonAsync($"/api/groups/{group}/messages", new { clientMessageId = Guid.NewGuid(), content = $"Pinned fixture {i}" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); var message = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(); expected.Add(message);
            Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsJsonAsync($"/api/groups/{group}/pins", new { messageId = message })).StatusCode);
        }
        var first = await member.GetFromJsonAsync<JsonElement>($"/api/groups/{group}/pins/page?limit=30"); Assert.Equal(30, first.GetProperty("items").GetArrayLength()); var cursor = first.GetProperty("nextCursor").GetInt64();
        var last = await member.GetFromJsonAsync<JsonElement>($"/api/groups/{group}/pins/page?limit=30&beforeSequence={cursor}"); Assert.Equal(5, last.GetProperty("items").GetArrayLength());
        var rows = first.GetProperty("items").EnumerateArray().Concat(last.GetProperty("items").EnumerateArray()).Select(row => row.GetProperty("messageId").GetGuid()).ToArray(); Assert.Equal(35, rows.Distinct().Count()); Assert.True(expected.SetEquals(rows));
        Assert.Equal(HttpStatusCode.NoContent, (await member.DeleteAsync($"/api/groups/{group}/for-me")).StatusCode);
        Assert.Empty((await member.GetFromJsonAsync<JsonElement>($"/api/groups/{group}/pins/page")).GetProperty("items").EnumerateArray());
        Assert.Equal(30, (await owner.GetFromJsonAsync<JsonElement>($"/api/groups/{group}/pins/page?limit=30")).GetProperty("items").GetArrayLength());
    }
    [Fact]
    public async Task Concurrent_link_revoke_and_join_have_serializable_outcomes_without_server_errors()
    {
        await using var factory = Factory(); using var owner = factory.CreateClient(); await MediaBoundaryIntegrationTests.Account(factory, owner);
        var create = await owner.PostAsJsonAsync("/api/groups", new { name = "Synthetic revoke race", inviteeUserIds = Array.Empty<Guid>() }); var group = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        for (var i = 0; i < 3; i++)
        {
            using var joiner = factory.CreateClient(); await MediaBoundaryIntegrationTests.Account(factory, joiner);
            var linkResponse = await owner.PostAsJsonAsync($"/api/groups/{group}/invite-links", new { expiresAt = (DateTime?)null }); Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode); var link = await linkResponse.Content.ReadFromJsonAsync<JsonElement>(); var token = link.GetProperty("token").GetString(); var linkId = link.GetProperty("id").GetGuid();
            var revoke = owner.DeleteAsync($"/api/groups/{group}/invite-links/{linkId}"); var join = joiner.PostAsJsonAsync("/api/group-invite-links/join", new { token, confirm = true }); await Task.WhenAll(revoke, join);
            var revoked = await revoke; var joined = await join; Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode); Assert.True(joined.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound, $"Unexpected join status {joined.StatusCode}");
            Assert.Equal(HttpStatusCode.NotFound, (await joiner.PostAsJsonAsync("/api/group-invite-links/join", new { token, confirm = true })).StatusCode);
        }
    }
}

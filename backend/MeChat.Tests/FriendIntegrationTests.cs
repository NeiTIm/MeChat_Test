using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MeChat.Tests;

public sealed class FriendIntegrationTests
{
    [Fact]
    public async Task Simultaneous_cross_requests_leave_one_friendship()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var a = factory.CreateClient();
        var b = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var first = await RegisterAndLogin(a, directory);
        var second = await RegisterAndLogin(b, directory);
        var responses = await Task.WhenAll(
            a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second }),
            b.PostAsJsonAsync("/api/friends/requests", new { targetUserId = first }));
        foreach (var response in responses)
            Assert.True(response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK, response.StatusCode.ToString());
        var friends = await (await a.GetAsync("/api/friends")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, friends.GetArrayLength());
    }

    [Fact]
    public async Task Rejection_allows_retry_and_request_action_is_owner_scoped()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var a = factory.CreateClient();
        var b = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var first = await RegisterAndLogin(a, directory);
        var second = await RegisterAndLogin(b, directory);
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second })).StatusCode);
        var requests = await (await b.GetAsync("/api/friends/requests")).Content.ReadFromJsonAsync<JsonElement>();
        var requestId = requests.GetProperty("incoming")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsync($"/api/friends/requests/{requestId}/reject", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsync($"/api/friends/requests/{requestId}/reject", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second })).StatusCode);
        Assert.Equal(0, (await (await a.GetAsync("/api/friends")).Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
    }

    [Fact]
    public async Task Reverse_request_autoaccepts_once_and_block_does_not_restore_friendship()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        var a = factory.CreateClient();
        var b = factory.CreateClient();
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var first = await RegisterAndLogin(a, directory);
        var second = await RegisterAndLogin(b, directory);

        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await b.PostAsJsonAsync("/api/friends/requests", new { targetUserId = first })).StatusCode);
        var friendList = await (await a.GetAsync("/api/friends")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, friendList.GetArrayLength());
        Assert.Equal(second, friendList[0].GetProperty("userId").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsJsonAsync("/api/blocks", new { targetUserId = second })).StatusCode);
        Assert.Equal(0, (await (await a.GetAsync("/api/friends")).Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync("/api/friends/requests", new { targetUserId = first })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync($"/api/blocks/{second}")).StatusCode);
        Assert.Equal(0, (await (await a.GetAsync("/api/friends")).Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
        Assert.Equal(HttpStatusCode.Created, (await b.PostAsJsonAsync("/api/friends/requests", new { targetUserId = first })).StatusCode);
    }

    private static async Task<Guid> RegisterAndLogin(HttpClient client, string directory)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"friend-{suffix}@example.invalid";
        var password = "SyntheticFriendPassword!2026";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { email, username = $"f{suffix}", password })).StatusCode);
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

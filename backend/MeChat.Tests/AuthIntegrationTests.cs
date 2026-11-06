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

public sealed class AuthIntegrationTests
{
    [Fact]
    public async Task Verification_is_required_and_refresh_replay_revokes_the_session()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"auth-{suffix}@example.invalid";
        var password = "SyntheticTestPassword!2026";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, username = $"user{suffix}", password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var unverified = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, unverified.StatusCode);

        var mailDirectory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var token = Directory.GetFiles(mailDirectory, "*.json")
            .Select(path => JsonDocument.Parse(File.ReadAllText(path)))
            .First(document => document.RootElement.GetProperty("email").GetString() == email)
            .RootElement.GetProperty("token").GetString();
        Assert.NotNull(token);
        var verify = await client.PostAsJsonAsync("/api/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password, deviceName = "Integration" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var access = body.GetProperty("accessToken").GetString();
        var cookies = login.Headers.GetValues("Set-Cookie").ToArray();
        var refresh = CookieValue(cookies, "webchat_refresh");
        var csrf = CookieValue(cookies, "webchat_csrf");

        using var sessionsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/sessions");
        sessionsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(sessionsRequest)).StatusCode);

        using var missingCsrf = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        missingCsrf.Headers.Add("Cookie", $"webchat_refresh={refresh}; webchat_csrf={csrf}");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(missingCsrf)).StatusCode);

        using var rotate = RefreshRequest(refresh, csrf);
        var rotated = await client.SendAsync(rotate);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var rotatedBody = await rotated.Content.ReadFromJsonAsync<JsonElement>();
        var rotatedAccess = rotatedBody.GetProperty("accessToken").GetString();

        using var replay = RefreshRequest(refresh, csrf);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(replay)).StatusCode);
        using var revokedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/sessions");
        revokedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rotatedAccess);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(revokedRequest)).StatusCode);
    }

    private static HttpRequestMessage RefreshRequest(string refresh, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"webchat_refresh={refresh}; webchat_csrf={csrf}");
        request.Headers.Add("X-CSRF-Token", csrf);
        return request;
    }

    private static string CookieValue(string[] values, string name) =>
        values.Select(value => value.Split(';')[0])
            .First(value => value.StartsWith(name + "=", StringComparison.Ordinal))[(name.Length + 1)..];
}

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

public sealed class UserIntegrationTests
{
    [Fact]
    public async Task Private_profile_privacy_username_cooldown_and_email_confirmation_are_enforced()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"profile-{suffix}@example.invalid";
        var nextEmail = $"profile-next-{suffix}@example.invalid";
        var username = $"p{suffix}";
        var password = "SyntheticProfilePassword!2026";

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { email, username, password })).StatusCode);
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        var verifyToken = MailToken(directory, email, "verify-email");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/verify-email", new { token = verifyToken })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var userId = body.GetProperty("userId").GetGuid();
        var access = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains(email, await me.Content.ReadAsStringAsync());
        var publicResponse = await client.GetAsync($"/api/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.DoesNotContain(email, await publicResponse.Content.ReadAsStringAsync());

        var privacy = await client.PatchAsJsonAsync("/api/users/me/privacy", new { showOnline = false, showLastSeen = false, allowStrangerMessages = false, allowGroupInvites = false });
        Assert.Equal(HttpStatusCode.OK, privacy.StatusCode);
        var privacyBody = await (await client.GetAsync("/api/users/me/privacy")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(privacyBody.GetProperty("allowGroupInvites").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync("/api/users/me/username", new { newUsername = $"changed{suffix}" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync("/api/users/me/username", new { newUsername = $"again{suffix}" })).StatusCode);

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/users/me/email-change", new { newEmail = nextEmail })).StatusCode);
        var before = await (await client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, before.GetProperty("email").GetString());
        var changeToken = MailToken(directory, nextEmail, "email-change");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/users/me/confirm-email-change", new { token = changeToken })).StatusCode);
        var after = await (await client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nextEmail, after.GetProperty("email").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/users/me/confirm-email-change", new { token = changeToken })).StatusCode);

        var search = await client.GetAsync($"/api/users/search?query=changed{suffix}&limit=1");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.DoesNotContain(nextEmail, await search.Content.ReadAsStringAsync());
    }

    private static string MailToken(string directory, string email, string purpose) =>
        Directory.GetFiles(directory, "*.json")
            .Select(path => JsonDocument.Parse(File.ReadAllText(path)))
            .First(document => document.RootElement.GetProperty("email").GetString() == email
                && document.RootElement.GetProperty("purpose").GetString() == purpose)
            .RootElement.GetProperty("token").GetString()!;
}

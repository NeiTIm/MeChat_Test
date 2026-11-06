using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MeChat.Tests;

public sealed class AuthLifecycleIntegrationTests
{
    [Fact]
    public async Task Password_change_preserves_current_device_reset_revokes_every_device_and_tokens_are_single_use()
    {
        await using var factory = Factory();
        using var first = factory.CreateClient(new() { HandleCookies = false });
        using var second = factory.CreateClient(new() { HandleCookies = false });
        var email = await Register(factory, first);
        var original = await Login(first, email, Password);
        var other = await Login(second, email, Password);
        first.DefaultRequestHeaders.Authorization = new("Bearer", original);
        second.DefaultRequestHeaders.Authorization = new("Bearer", other);
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "WrongPassword!2026", newPassword = NextPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/api/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = Password, newPassword = NextPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/api/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = Password })).StatusCode);
        await Login(second, email, NextPassword);
        Assert.Equal(HttpStatusCode.OK, (await first.PostAsJsonAsync("/api/auth/forgot-password", new { email })).StatusCode);
        var token = MailToken(factory, email, "reset-password");
        Assert.Equal(HttpStatusCode.NoContent, (await first.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = ResetPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = NextPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = NextPassword })).StatusCode);
        await Login(first, email, ResetPassword);
    }

    [Fact]
    public async Task Self_deactivation_revokes_all_sessions_and_reactivation_requires_password_and_new_login()
    {
        await using var factory = Factory();
        using var first = factory.CreateClient();
        using var second = factory.CreateClient();
        var email = await Register(factory, first);
        var original = await Login(first, email, Password);
        await Login(second, email, Password);
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsJsonAsync("/api/auth/deactivate", new { password = "WrongPassword!2026" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.PostAsJsonAsync("/api/auth/deactivate", new { password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.PostAsJsonAsync("/api/auth/reactivate", new { identifier = email, password = "WrongPassword!2026" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.PostAsJsonAsync("/api/auth/reactivate", new { identifier = email, password = Password })).StatusCode);
        // Reactivation must not resurrect the revoked JWT.
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/auth/sessions")).StatusCode);
        var fresh = await Login(first, email, Password);
        Assert.NotEqual(original, fresh);
        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/api/auth/sessions")).StatusCode);
    }

    [Fact]
    public async Task Fifth_failed_login_locks_until_exact_fifteen_minute_boundary()
    {
        var clock = new MutableClock();
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        var email = await Register(factory, client);
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = "WrongPassword!2026" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = Password })).StatusCode);
        clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromMilliseconds(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password = Password })).StatusCode);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await Login(client, email, Password);
    }

    private const string Password = "SyntheticLifecycle!2026";
    private const string NextPassword = "SyntheticChanged!2026";
    private const string ResetPassword = "SyntheticReset!2026";
    private static WebApplicationFactory<Program> Factory(TimeProvider? clock = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) }));
            if (clock is not null) builder.ConfigureServices(services => { services.RemoveAll<TimeProvider>(); services.AddSingleton(clock); });
        });
    private static async Task<string> Register(WebApplicationFactory<Program> factory, HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"lifecycle-{suffix}@example.invalid";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { email, username = $"lc{suffix}", password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/verify-email", new { token = MailToken(factory, email, "verify-email") })).StatusCode);
        return email;
    }
    private static async Task<string> Login(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }
    private static string MailToken(WebApplicationFactory<Program> factory, string email, string purpose)
    {
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        foreach (var path in Directory.GetFiles(directory, "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.GetProperty("email").GetString() == email && document.RootElement.GetProperty("purpose").GetString() == purpose)
                return document.RootElement.GetProperty("token").GetString()!;
        }
        throw new InvalidOperationException("Synthetic mail not found.");
    }
    private sealed class MutableClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan elapsed) => now += elapsed;
    }
}

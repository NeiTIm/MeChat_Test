using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MeChat.Tests;

public sealed class MediaBoundaryIntegrationTests
{
    [Fact]
    public async Task Multipart_limits_pending_ownership_five_attachment_cap_and_current_download_rights()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) })));
        using var owner = factory.CreateClient(); using var other = factory.CreateClient();
        var a = await Account(factory, owner); var b = await Account(factory, other);
        async Task<HttpResponseMessage> Upload(byte[] bytes, string name, string purpose = "Message")
        {
            using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(bytes), "file", name); form.Add(new StringContent(purpose), "purpose");
            return await owner.PostAsync("/api/files", form);
        }
        var exact = Encoding.ASCII.GetBytes(new string('a', 25 * 1024 * 1024));
        var uploaded = await Upload(exact, "exact.txt"); Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var file = await uploaded.Content.ReadFromJsonAsync<JsonElement>(); var fileId = file.GetProperty("id").GetGuid();
        Assert.Equal(exact.Length, file.GetProperty("sizeBytes").GetInt64());
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(new byte[25 * 1024 * 1024 + 1], "excess.txt")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(new byte[10 * 1024 * 1024 + 1], "excess.png")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(new byte[5 * 1024 * 1024 + 1], "excess.png", "Avatar")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(Encoding.ASCII.GetBytes("not an image"), "spoof.png")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload([255, 255], "invalid.txt")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload([], "empty.txt")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload([65], "forbidden.exe")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload([65], "avatar.txt", "Avatar")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload([65], "purpose.txt", "Unknown")).StatusCode);
        foreach (var (size, purpose) in new[] { (5 * 1024 * 1024, "Avatar"), (10 * 1024 * 1024, "Message") })
        {
            var valid = await Upload(PaddedPng(size), "exact.png", purpose); Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
            Assert.Equal(size, (await valid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sizeBytes").GetInt64());
        }
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/files/{fileId}/download")).StatusCode);
        Assert.Equal(exact, await owner.GetByteArrayAsync($"/api/files/{fileId}/download"));
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync("/api/friends/requests", new { targetUserId = b })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.PostAsJsonAsync("/api/friends/requests", new { targetUserId = a })).StatusCode);
        var conversation = (await (await owner.PostAsJsonAsync("/api/conversations/private", new { targetUserId = b })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var ids = new List<Guid> { fileId };
        for (var i = 0; i < 5; i++) { var response = await Upload([65], $"small-{i}.txt"); Assert.Equal(HttpStatusCode.OK, response.StatusCode); ids.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid()); }
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/conversations/{conversation}/messages", new { clientMessageId = Guid.NewGuid(), content = "stolen pending", attachmentIds = new[] { fileId } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync($"/api/conversations/{conversation}/messages", new { clientMessageId = Guid.NewGuid(), content = "six files", attachmentIds = ids })).StatusCode);
        var sent = await owner.PostAsJsonAsync($"/api/conversations/{conversation}/messages", new { clientMessageId = Guid.NewGuid(), content = "five files", attachmentIds = ids.Take(5) });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode); var message = (await sent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(exact, await other.GetByteArrayAsync($"/api/files/{fileId}/download"));
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsync($"/api/messages/{message}/recall", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/files/{fileId}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/files/{fileId}/download")).StatusCode);
    }
    [Fact]
    public async Task Jwt_issuer_audience_signature_expiry_session_and_live_admin_checks_fail_closed()
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = secret })));
        using var client = factory.CreateClient(); var id = await Account(factory, client);
        var original = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var reader = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler(); var parsed = reader.ReadJwtToken(original); var sid = parsed.Claims.Single(c => c.Type == "sid").Value;
        string Token(string issuer = "MeChat.Api", string audience = "MeChat.Frontend", string? key = null, DateTime? expiry = null, string? session = null)
        {
            var claims = new[] { new System.Security.Claims.Claim("sub", id.ToString()), new System.Security.Claims.Claim("sid", session ?? sid), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin") };
            return reader.WriteToken(new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(issuer, audience, claims, expires: expiry ?? DateTime.UtcNow.AddMinutes(5), signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? secret)), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)));
        }
        foreach (var bad in new[] { Token(issuer: "WrongIssuer"), Token(audience: "WrongAudience"), Token(key: Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))), Token(expiry: DateTime.UtcNow.AddMinutes(-2)), Token(session: Guid.NewGuid().ToString()), "not-a-jwt" })
        { client.DefaultRequestHeaders.Authorization = new("Bearer", bad); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode); }
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/dashboard")).StatusCode);
    }
    [Fact]
    public async Task Concurrent_same_client_message_id_has_one_canonical_row_and_notification()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) })));
        using var a = factory.CreateClient(); using var b = factory.CreateClient(); var first = await Account(factory, a); var second = await Account(factory, b);
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/friends/requests", new { targetUserId = second })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await b.PostAsJsonAsync("/api/friends/requests", new { targetUserId = first })).StatusCode);
        var conversation = (await (await a.PostAsJsonAsync("/api/conversations/private", new { targetUserId = second })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var clientMessageId = Guid.NewGuid();
        var sends = await Task.WhenAll(Enumerable.Range(0, 3).Select(i => a.PostAsJsonAsync($"/api/conversations/{conversation}/messages", new { clientMessageId, content = $"race payload {i}" })));
        Assert.All(sends, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var canonical = new HashSet<Guid>(); foreach (var response in sends) canonical.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid()); Assert.Single(canonical);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MeChat.Api.Infrastructure.Persistence.AppDbContext>();
        Assert.Equal(1, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Messages.Where(m => m.ConversationId == conversation && m.ClientMessageId == clientMessageId)));
        Assert.Equal(1, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Notifications.Where(n => n.RecipientUserId == second && n.ReferenceId == canonical.Single())));
    }
    private static byte[] PaddedPng(int size)
    {
        var image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=");
        var result = new byte[size]; var offset = image.Length - 12; Array.Copy(image, result, offset);
        var length = size - image.Length - 12; System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset, 4), length);
        Encoding.ASCII.GetBytes("tEXt").CopyTo(result, offset + 4); result[offset + 8] = (byte)'k'; result[offset + 9] = 0;
        result.AsSpan(offset + 10, length - 2).Fill((byte)'a');
        uint crc = 0xffffffff;
        foreach (var value in result.AsSpan(offset + 4, 4 + length)) { crc ^= value; for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1; }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset + 8 + length, 4), ~crc);
        Array.Copy(image, image.Length - 12, result, size - 12, 12); return result;
    }
    internal static async Task<Guid> Account(WebApplicationFactory<Program> factory, HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N"); var email = $"media-qa-{suffix}@example.invalid"; const string password = "SyntheticMediaBoundary!2026";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new { email, username = $"mb{suffix}", password })).StatusCode);
        var directory = Path.Combine(factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath, "private-uploads", "dev-mail");
        string? token = null;
        foreach (var path in Directory.GetFiles(directory, "*.json")) { using var mail = JsonDocument.Parse(File.ReadAllText(path)); if (mail.RootElement.GetProperty("email").GetString() == email) { token = mail.RootElement.GetProperty("token").GetString(); break; } }
        Assert.NotNull(token); Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/verify-email", new { token })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { identifier = email, password }); Assert.Equal(HttpStatusCode.OK, login.StatusCode); var data = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("accessToken").GetString()); return data.GetProperty("userId").GetGuid();
    }
}

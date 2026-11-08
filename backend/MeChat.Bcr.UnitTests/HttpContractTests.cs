using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MeChat.Api.Configurations;
using MeChat.Api.Features.Notifications;
using MeChat.Api.Features.Notifications.Models;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;
using MeChat.Api.Features.Friends;
using MeChat.Api.Features.Friends.Repositories.Interfaces;

namespace MeChat.Bcr.UnitTests;

// In-process HTTP, test authentication and repository doubles. No Program/DbContext/SQL.
// These verify actual MVC binding/filter contracts, not JWT or SQL integration acceptance.
public sealed class HttpContractTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static async Task<IHost> Start(bool enabled)
    {
        var hub = new RecordingHub();
        return await new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddAuthentication("BcrUnit").AddScheme<AuthenticationSchemeOptions, UnitAuth>("BcrUnit", _ => { });
            services.AddAuthorization();
            services.AddControllers().AddApplicationPart(typeof(NotificationsController).Assembly);
            services.Configure<BcrFeatureOptions>(o => { o.NotificationLifecycleEnabled = enabled; o.FriendRequestSeenEnabled = enabled; });
            var notices = StubProxy.Create<INotificationRepository>((method, _) => method.Name switch
            {
                "GetNotificationPageAsync" or "GetTrashPageAsync" or "GetUnreadNotificationsAsync" => Task.FromResult(new List<Notification>()),
                "GetLifecycleNotificationAsync" or "GetNotificationAsync" => Task.FromResult<Notification?>(null),
                _ => Task.FromResult(0)
            });
            var friends = StubProxy.Create<IFriendRepository>((method, args) => method.Name switch
            {
                "GetUnseenCountAsync" => Task.FromResult(3),
                "MarkSeenAsync" => Task.FromResult(0),
                "ExecuteAtomicAsync" => ((Func<Task<int>>)args[0]!)(),
                _ => throw new NotSupportedException(method.Name)
            });
            services.AddSingleton(new NotificationService(notices, null!, null!, null!, new FixedClock(), hub.Events));
            services.AddSingleton(new FriendService(friends, new FixedClock(), hub.Events));
        }).Configure(app =>
        {
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(e => e.MapControllers());
        })).StartAsync();
    }

    [Theory]
    [InlineData("GET", "/api/notifications/trash?before=invalid")]
    [InlineData("DELETE", "/api/notifications/read")]
    [InlineData("GET", "/api/friends/requests/unread-count")]
    [InlineData("POST", "/api/friends/requests/mark-seen")]
    public async Task Disabled_endpoint_precedes_binding_and_never_calls_repository(string method, string url)
    {
        using var host = await Start(false); using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = new StringContent("malformed-test-body", Encoding.UTF8, "application/json") };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); Assert.Contains("BCR_FEATURE_DISABLED", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"requestIds\":null}")]
    [InlineData("{\"requestIds\":[]}")]
    [InlineData("{\"requestIds\":[\"00000000-0000-0000-0000-000000000000\"]}")]
    [InlineData("{\"requestIds\":[\"do-not-echo-test-value\"]}")]
    [InlineData("{")]
    public async Task Bad_batch_has_safe_400_without_echo(string body)
    {
        using var host = await Start(true); using var client = host.GetTestClient();
        var response = await client.PostAsync("/api/friends/requests/mark-seen", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(); Assert.Contains("INVALID_REQUEST_IDS", json); Assert.DoesNotContain("do-not-echo-test-value", json);
    }

    [Theory]
    [InlineData("before=2026-11-08T10:00:00Z")]
    [InlineData("beforeId=00000000-0000-0000-0000-000000000001")]
    [InlineData("before=&beforeId=")]
    [InlineData("before=do-not-echo-test-value&beforeId=invalid-test-id")]
    [InlineData("limit=do-not-echo-test-value")]
    public async Task Trash_pair_validation_is_local_and_safe(string query)
    {
        using var host = await Start(true); using var client = host.GetTestClient();
        var response = await client.GetAsync("/api/notifications/trash?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(); Assert.Contains("INVALID_NOTIFICATION_CURSOR", json); Assert.DoesNotContain("do-not-echo-test-value", json);
    }

    [Fact]
    public async Task Active_legacy_before_only_and_new_count_shapes_stay_compatible()
    {
        using var host = await Start(true); using var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/notifications?before=2026-11-08T10:00:00Z")).StatusCode);
        Assert.Equal("{\"unreadCount\":3}", await client.GetStringAsync("/api/friends/requests/unread-count"));
        Assert.Equal("{\"count\":0}", await client.GetStringAsync("/api/notifications/unread-count"));
    }

    [Fact]
    public async Task Missing_auth_is_denied_before_feature_filter()
    {
        using var host = await Start(false); using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Unauthenticated", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/notifications/trash")).StatusCode);
    }

    [Fact]
    public async Task Valid_duplicate_unknown_batch_is_empty_204_and_non_json_is_415()
    {
        using var host = await Start(true); using var client = host.GetTestClient(); var id = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/api/friends/requests/mark-seen", new { requestIds = new[] { id, id }, receiverUserId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); Assert.Empty(await response.Content.ReadAsStringAsync());
        var unsupported = await client.PostAsync("/api/friends/requests/mark-seen", new StringContent("test-only", Encoding.UTF8, "text/plain"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
    }

    public sealed class UnitAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(Request.Headers.ContainsKey("X-Test-Unauthenticated")
            ? AuthenticateResult.NoResult()
            : AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Actor.ToString())], Scheme.Name)), Scheme.Name)));
    }
}

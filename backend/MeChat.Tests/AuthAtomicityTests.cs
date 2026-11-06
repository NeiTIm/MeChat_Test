using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Auth;
using MeChat.Api.Realtime;

namespace MeChat.Tests;

public sealed class AuthAtomicityTests
{
    private const string Password = "SyntheticAtomicity!2026";
    private readonly IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) }).Build();
    private AuthService Service(AppDbContext db, Mail mail) => new(db, config, mail, TimeProvider.System, new SessionConnections());
    private static AppDbContext Db(SaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(DatabaseTarget.Validate(DatabaseTarget.DefaultConnection));
        if (interceptor is not null) options.AddInterceptors(interceptor);
        return new(options.Options);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Token_claim_and_user_mutation_rollback_together_after_a_save_failure(bool reset)
    {
        var mail = new Mail(); var email = $"atomic-{Guid.NewGuid():N}@example.invalid";
        var fault = new FailUserSave(); string raw;
        await using (var db = Db(fault))
        {
            var auth = Service(db, mail);
            Assert.True(await auth.RegisterAsync(new(email, $"at{Guid.NewGuid():N}", Password), default));
            if (reset) { Assert.True(await auth.VerifyEmailAsync(mail.Token, default)); await auth.ForgotPasswordAsync(email, default); }
            raw = mail.Token; fault.Armed = true;
            await Assert.ThrowsAsync<IOException>(async () =>
            {
                if (reset) await auth.ResetPasswordAsync(new(raw, "SyntheticResetAtomic!2026"), default);
                else await auth.VerifyEmailAsync(raw, default);
            });
        }
        await using var inspect = Db();
        var user = await inspect.Users.SingleAsync(u => u.Email == email);
        var token = await inspect.AuthTokens.SingleAsync(t => t.UserId == user.Id && t.Type == (reset ? "PasswordReset" : "EmailVerification"));
        Assert.Null(token.UsedAt);
        var retry = Service(inspect, mail);
        Assert.True(reset ? await retry.ResetPasswordAsync(new(raw, "SyntheticResetAtomic!2026"), default) : await retry.VerifyEmailAsync(raw, default));
        Assert.False(reset ? await retry.ResetPasswordAsync(new(raw, Password), default) : await retry.VerifyEmailAsync(raw, default));
    }
    [Fact]
    public async Task Concurrent_failed_logins_all_count_toward_one_lockout_without_server_errors()
    {
        var mail = new Mail(); var email = $"lock-race-{Guid.NewGuid():N}@example.invalid";
        await using (var setup = Db()) { var auth = Service(setup, mail); Assert.True(await auth.RegisterAsync(new(email, $"lr{Guid.NewGuid():N}", Password), default)); Assert.True(await auth.VerifyEmailAsync(mail.Token, default)); }
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var db = Db(); return await Service(db, mail).LoginAsync(new(email, "InvalidSyntheticPassword!", "race"), default);
        }));
        Assert.All(results, result => Assert.Null(result));
        await using var inspect = Db();
        var user = await inspect.Users.SingleAsync(u => u.Email == email);
        Assert.True(user.LockoutUntil > DateTime.UtcNow);
        Assert.Null(await Service(inspect, mail).LoginAsync(new(email, Password, "correct-during-lock"), default));
    }
    [Fact]
    public async Task Concurrent_registration_of_identical_identity_has_one_user_and_generic_acknowledgements()
    {
        var email = $"register-race-{Guid.NewGuid():N}@example.invalid"; var username = $"rr{Guid.NewGuid():N}";
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ => { await using var db = Db(); return await Service(db, new Mail()).RegisterAsync(new(email, username, Password), default); }));
        Assert.All(results, Assert.True);
        await using var inspect = Db(); Assert.Equal(1, await inspect.Users.CountAsync(u => u.Email == email));
    }
    [Fact]
    public async Task Concurrent_refresh_replay_revokes_the_winning_rotation_session()
    {
        var mail = new Mail(); var email = $"refresh-race-{Guid.NewGuid():N}@example.invalid"; string refresh; Guid actor;
        await using (var setup = Db())
        {
            var auth = Service(setup, mail); Assert.True(await auth.RegisterAsync(new(email, $"rf{Guid.NewGuid():N}", Password), default)); Assert.True(await auth.VerifyEmailAsync(mail.Token, default));
            var login = await auth.LoginAsync(new(email, Password, "race"), default); Assert.NotNull(login); refresh = login.Value.Refresh; actor = login.Value.Access.UserId;
        }
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ => { await using var db = Db(); return await Service(db, mail).RefreshAsync(refresh, default); }));
        Assert.Single(results, r => r is not null); Assert.Single(results, r => r is null);
        await using var check = Db(); Assert.False(await check.UserSessions.AnyAsync(s => s.UserId == actor && s.RevokedAt == null));
        var winner = results.Single(r => r is not null)!.Value; Assert.Null(await Service(check, mail).RefreshAsync(winner.Refresh, default));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Single_use_auth_tokens_are_rejected_at_the_exact_expiry_boundary(bool reset)
    {
        var mail = new Mail(); var clock = new Clock(); var email = $"expiry-{Guid.NewGuid():N}@example.invalid";
        await using var db = Db(); var auth = new AuthService(db, config, mail, clock, new SessionConnections());
        Assert.True(await auth.RegisterAsync(new(email, $"ex{Guid.NewGuid():N}", Password), default));
        if (reset) { Assert.True(await auth.VerifyEmailAsync(mail.Token, default)); await auth.ForgotPasswordAsync(email, default); }
        var token = mail.Token; clock.Now = clock.Now.AddHours(reset ? 1 : 24);
        Assert.False(reset ? await auth.ResetPasswordAsync(new(token, Password), default) : await auth.VerifyEmailAsync(token, default));
        var user = await db.Users.SingleAsync(u => u.Email == email); Assert.Null((await db.AuthTokens.SingleAsync(t => t.UserId == user.Id && t.Type == (reset ? "PasswordReset" : "EmailVerification"))).UsedAt);
    }
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    [Fact]
    public async Task Email_change_claim_rolls_back_when_the_profile_save_fails()
    {
        var mail = new Mail(); var email = $"email-fault-{Guid.NewGuid():N}@example.invalid"; var next = $"email-next-{Guid.NewGuid():N}@example.invalid"; var fault = new FailUserSave(); Guid actor; string token;
        await using (var db = Db(fault))
        {
            var auth = Service(db, mail); Assert.True(await auth.RegisterAsync(new(email, $"ef{Guid.NewGuid():N}", Password), default)); Assert.True(await auth.VerifyEmailAsync(mail.Token, default)); actor = (await db.Users.SingleAsync(u => u.Email == email)).Id;
            var users = new MeChat.Api.Features.Users.UserService(db, mail, TimeProvider.System); Assert.True(await users.RequestEmailChangeAsync(actor, next, default)); token = mail.Token; fault.Armed = true;
            await Assert.ThrowsAsync<IOException>(() => users.ConfirmEmailChangeAsync(token, actor, default));
        }
        await using var check = Db(); Assert.Null((await check.AuthTokens.SingleAsync(t => t.UserId == actor && t.Type == "EmailChange")).UsedAt); Assert.Equal(email, (await check.Users.SingleAsync(u => u.Id == actor)).Email);
        Assert.True(await new MeChat.Api.Features.Users.UserService(check, mail, TimeProvider.System).ConfirmEmailChangeAsync(token, actor, default));
    }
    [Fact]
    public async Task Username_change_is_allowed_at_exact_thirty_day_boundary()
    {
        var mail = new Mail(); var clock = new Clock(); var email = $"name-boundary-{Guid.NewGuid():N}@example.invalid";
        await using var db = Db(); var auth = Service(db, mail); Assert.True(await auth.RegisterAsync(new(email, $"nb{Guid.NewGuid():N}", Password), default)); var actor = (await db.Users.SingleAsync(u => u.Email == email)).Id;
        var users = new MeChat.Api.Features.Users.UserService(db, mail, clock); Assert.True(await users.ChangeUsernameAsync(actor, $"first{Guid.NewGuid():N}", default));
        clock.Now = clock.Now.AddDays(30).AddTicks(-1); Assert.False(await users.ChangeUsernameAsync(actor, $"early{Guid.NewGuid():N}", default));
        clock.Now = clock.Now.AddTicks(1); Assert.True(await users.ChangeUsernameAsync(actor, $"exact{Guid.NewGuid():N}", default));
    }
    [Fact]
    public async Task Concurrent_username_changes_have_one_winner_and_do_not_bypass_cooldown()
    {
        var mail = new Mail(); var email = $"username-race-{Guid.NewGuid():N}@example.invalid"; Guid actor;
        await using (var setup = Db()) { Assert.True(await Service(setup, mail).RegisterAsync(new(email, $"nr{Guid.NewGuid():N}", Password), default)); actor = (await setup.Users.SingleAsync(u => u.Email == email)).Id; }
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ => { await using var db = Db(); return await new MeChat.Api.Features.Users.UserService(db, mail, TimeProvider.System).ChangeUsernameAsync(actor, $"new{Guid.NewGuid():N}", default); }));
        Assert.Single(results, value => value); Assert.Single(results, value => !value);
    }
    private sealed class Mail : IAuthMailSender
    {
        public string Token { get; private set; } = "";
        public Task SendTokenAsync(string email, string token, string purpose, CancellationToken ct) { Token = token; return Task.CompletedTask; }
    }
    private sealed class FailUserSave : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (Armed && e.Context!.ChangeTracker.Entries<User>().Any(row => row.State == EntityState.Modified)) { Armed = false; throw new IOException("Synthetic failure before user save."); }
            return base.SavingChangesAsync(e, result, ct);
        }
    }
}

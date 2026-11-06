
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MeChat.Api.Features.Auth.Repositories.Interfaces;
using MeChat.Api.Realtime;

namespace MeChat.Api.Features.Auth;

public sealed class AuthService(
    IAuthRepository repository,
    IConfiguration configuration,
    IAuthMailSender mail,
    TimeProvider clock,
    SessionConnections connections)
{
    private readonly PasswordHasher<User> _hasher = new();

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // REGISTER
    // =====================================================

    public Task<bool> RegisterAsync(
        RegisterRequest input,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(async afterCommit =>
        {
            var email = input.Email.Trim().ToUpperInvariant();
            var username = input.Username.Trim().ToUpperInvariant();

            if (email.Length > 320 ||
                username.Length > 50 ||
                username.Length < 3 ||
                input.Password.Length < 12)
            {
                return false;
            }

            if (await repository.UserExistsAsync(
                    email, username, ct))
            {
                return true;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = input.Email.Trim(),
                NormalizedEmail = email,
                Username = input.Username.Trim(),
                NormalizedUsername = username,
                DisplayName = input.Username.Trim(),
                CreatedAt = Now
            };

            user.PasswordHash =
                _hasher.HashPassword(user, input.Password);

            repository.AddUser(user);

            repository.AddPrivacySetting(
                new UserPrivacySetting
                {
                    UserId = user.Id
                });

            var token = NewToken();

            repository.AddAuthToken(new AuthToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = "EmailVerification",
                TokenHash = Hash(token),
                ExpiresAt = Now.AddHours(24)
            });

            await repository.SaveChangesAsync(ct);

            afterCommit.Add(() =>
                mail.SendTokenAsync(
                    user.Email,
                    token,
                    "verify-email",
                    ct));

            return true;
        }, ct);
    }

    // =====================================================
    // VERIFY EMAIL
    // =====================================================

    public Task<bool> VerifyEmailAsync(
        string rawToken,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(async _ =>
        {
            var hash = Hash(rawToken);

            var token =
                await repository.GetAuthTokenForUpdateAsync(
                    hash,
                    "EmailVerification",
                    ct);

            if (token is null ||
                token.UsedAt is not null ||
                token.ExpiresAt <= Now)
            {
                return false;
            }

            var consumed =
                await repository.TryConsumeAuthTokenAsync(
                    token.Id,
                    Now,
                    ct);

            if (!consumed)
                return false;

            var user =
                await repository.GetUserForUpdateAsync(
                    token.UserId,
                    ct);

            if (user is null)
                return false;

            user.EmailVerifiedAt = Now;

            await repository.SaveChangesAsync(ct);

            return true;
        }, ct);
    }

    // =====================================================
    // RESEND VERIFICATION
    // =====================================================

    public async Task ResendVerificationAsync(
        string email,
        CancellationToken ct)
    {
        await AtomicAsync<bool>(async afterCommit =>
        {
            var normalized =
                email.Trim().ToUpperInvariant();

            var user =
                await repository.GetUserByEmailAsync(
                    normalized,
                    ct);

            if (user is null ||
                user.EmailVerifiedAt is not null)
            {
                return true;
            }

            var token = NewToken();

            repository.AddAuthToken(new AuthToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = "EmailVerification",
                TokenHash = Hash(token),
                ExpiresAt = Now.AddHours(24)
            });

            await repository.SaveChangesAsync(ct);

            afterCommit.Add(() =>
                mail.SendTokenAsync(
                    user.Email,
                    token,
                    "verify-email",
                    ct));

            return true;
        }, ct);
    }

    // =====================================================
    // FORGOT PASSWORD
    // =====================================================

    public async Task ForgotPasswordAsync(
        string email,
        CancellationToken ct)
    {
        await AtomicAsync<bool>(async afterCommit =>
        {
            var normalized =
                email.Trim().ToUpperInvariant();

            var user =
                await repository.GetUserByEmailAsync(
                    normalized,
                    ct);

            if (user is null ||
                user.EmailVerifiedAt is null)
            {
                return true;
            }

            var token = NewToken();

            repository.AddAuthToken(new AuthToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = "PasswordReset",
                TokenHash = Hash(token),
                ExpiresAt = Now.AddHours(1)
            });

            await repository.SaveChangesAsync(ct);

            afterCommit.Add(() =>
                mail.SendTokenAsync(
                    user.Email,
                    token,
                    "reset-password",
                    ct));

            return true;
        }, ct);
    }

    // =====================================================
    // RESET PASSWORD
    // =====================================================

    public Task<bool> ResetPasswordAsync(
        ResetPasswordRequest input,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(async afterCommit =>
        {
            var hash = Hash(input.Token);

            var token =
                await repository.GetAuthTokenForUpdateAsync(
                    hash,
                    "PasswordReset",
                    ct);

            if (token is null ||
                token.UsedAt is not null ||
                token.ExpiresAt <= Now)
            {
                return false;
            }

            var consumed =
                await repository.TryConsumeAuthTokenAsync(
                    token.Id,
                    Now,
                    ct);

            if (!consumed)
                return false;

            var user =
                await repository.GetUserForUpdateAsync(
                    token.UserId,
                    ct);

            if (user is null)
                return false;

            user.PasswordHash =
                _hasher.HashPassword(
                    user,
                    input.NewPassword);

            user.FailedLoginCount = 0;
            user.LockoutUntil = null;

            await repository.RevokeAllSessionsAsync(
                user.Id,
                Now,
                ct);

            await repository.SaveChangesAsync(ct);

            afterCommit.Add(() =>
            {
                connections.Revoke(user.Id);
                return Task.CompletedTask;
            });

            return true;
        }, ct);
    }

    // =====================================================
    // LOGIN
    // =====================================================

    public Task<(AccessResponse Access, string Refresh)?>
        LoginAsync(
            LoginRequest input,
            CancellationToken ct)
    {
        return AtomicAsync<
            (AccessResponse Access, string Refresh)?>(
            async _ =>
            {
                var normalized = input.Identifier
                    .Trim()
                    .ToUpperInvariant();

                var user =
                    await repository
                        .GetUserByIdentifierForUpdateAsync(
                            normalized,
                            ct);

                if (user is null)
                {
                    var dummy = new User();

                    var dummyHash =
                        _hasher.HashPassword(
                            dummy,
                            "invalid-password-placeholder");

                    _hasher.VerifyHashedPassword(
                        dummy,
                        dummyHash,
                        input.Password);

                    return null;
                }

                if (user.LockoutUntil > Now)
                    return null;

                var result =
                    _hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        input.Password);

                if (result ==
                    PasswordVerificationResult.Failed)
                {
                    user.FailedLoginCount++;

                    if (user.FailedLoginCount >= 5)
                    {
                        user.FailedLoginCount = 0;
                        user.LockoutUntil =
                            Now.AddMinutes(15);
                    }

                    await repository.SaveChangesAsync(ct);

                    return null;
                }

                if (user.EmailVerifiedAt is null ||
                    user.DeactivatedAt is not null ||
                    await IsSuspendedAsync(user.Id, ct))
                {
                    return null;
                }

                user.FailedLoginCount = 0;
                user.LockoutUntil = null;

                if (result ==
                    PasswordVerificationResult
                        .SuccessRehashNeeded)
                {
                    user.PasswordHash =
                        _hasher.HashPassword(
                            user,
                            input.Password);
                }

                var deviceName =
                    input.DeviceName ?? "Browser";

                deviceName = deviceName[
                    ..Math.Min(deviceName.Length, 100)];

                var session = new UserSession
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    DeviceName = deviceName,
                    CreatedAt = Now,
                    LastActivityAt = Now
                };

                var refresh = NewToken();

                repository.AddSession(session);

                repository.AddRefreshToken(
                    new RefreshToken
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        TokenHash = Hash(refresh),
                        ExpiresAt = Now.AddDays(30)
                    });

                await repository.SaveChangesAsync(ct);

                return (
                    IssueAccess(user, session.Id),
                    refresh
                );
            },
            ct);
    }

    // =====================================================
    // REFRESH TOKEN
    // =====================================================

    public Task<(AccessResponse Access, string Refresh)?>
        RefreshAsync(
            string rawToken,
            CancellationToken ct)
    {
        return AtomicAsync<
            (AccessResponse Access, string Refresh)?>(
            async afterCommit =>
            {
                var hash = Hash(rawToken);

                var old =
                    await repository.GetRefreshTokenAsync(
                        hash,
                        ct);

                if (old is null)
                    return null;

                var session =
                    await repository.GetSessionAsync(
                        old.SessionId,
                        ct);

                if (session is null ||
                    session.RevokedAt is not null)
                {
                    return null;
                }

                if (old.RevokedAt is not null ||
                    old.ExpiresAt <= Now)
                {
                    session.RevokedAt = Now;

                    await repository.SaveChangesAsync(ct);

                    afterCommit.Add(() =>
                    {
                        connections.Revoke(
                            session.UserId,
                            session.Id);

                        return Task.CompletedTask;
                    });

                    return null;
                }

                var user =
                    await repository.GetUserForUpdateAsync(
                        session.UserId,
                        ct);

                if (user is null ||
                    user.EmailVerifiedAt is null ||
                    user.DeactivatedAt is not null ||
                    await IsSuspendedAsync(user.Id, ct))
                {
                    return null;
                }

                var claimed =
                    await repository.TryRevokeRefreshTokenAsync(
                        old.Id,
                        Now,
                        ct);

                if (!claimed)
                {
                    session.RevokedAt = Now;

                    await repository.SaveChangesAsync(ct);

                    afterCommit.Add(() =>
                    {
                        connections.Revoke(
                            session.UserId,
                            session.Id);

                        return Task.CompletedTask;
                    });

                    return null;
                }

                var refresh = NewToken();

                var replacement = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    TokenHash = Hash(refresh),
                    ExpiresAt = Now.AddDays(30)
                };

                repository.AddRefreshToken(replacement);

                old.ReplacedByTokenId = replacement.Id;

                session.LastActivityAt = Now;

                await repository.SaveChangesAsync(ct);

                return (
                    IssueAccess(user, session.Id),
                    refresh
                );
            },
            ct);
    }

    // =====================================================
    // LOGOUT
    // =====================================================

    public Task<bool> LogoutAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(async afterCommit =>
        {
            await repository.GetUserForUpdateAsync(
                userId,
                ct);

            var changed =
                await repository.RevokeSessionAsync(
                    userId,
                    sessionId,
                    Now,
                    ct);

            afterCommit.Add(() =>
            {
                connections.Revoke(
                    userId,
                    sessionId);

                return Task.CompletedTask;
            });

            return changed == 1;
        }, ct);
    }

    // =====================================================
    // LOGOUT OTHER SESSIONS
    // =====================================================

    public Task<int> LogoutOthersAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct)
    {
        return AtomicAsync<int>(async afterCommit =>
        {
            await repository.GetUserForUpdateAsync(
                userId,
                ct);

            var changed =
                await repository.RevokeOtherSessionsAsync(
                    userId,
                    currentSessionId,
                    Now,
                    ct);

            afterCommit.Add(() =>
            {
                connections.Revoke(
                    userId,
                    exceptSession: currentSessionId);

                return Task.CompletedTask;
            });

            return changed;
        }, ct);
    }

    // =====================================================
    // REVOKE SESSION
    // =====================================================

    public Task<bool> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct)
    {
        return LogoutAsync(
            userId,
            sessionId,
            ct);
    }

    // =====================================================
    // CHANGE PASSWORD
    // =====================================================

    public Task<bool> ChangePasswordAsync(
        Guid userId,
        Guid currentSessionId,
        ChangePasswordRequest input,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(async afterCommit =>
        {
            var user =
                await repository.GetUserForUpdateAsync(
                    userId,
                    ct);

            if (user is null)
                return false;

            var result =
                _hasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    input.CurrentPassword);

            if (result ==
                PasswordVerificationResult.Failed)
            {
                return false;
            }

            user.PasswordHash =
                _hasher.HashPassword(
                    user,
                    input.NewPassword);

            await repository.RevokeOtherSessionsAsync(
                userId,
                currentSessionId,
                Now,
                ct);

            await repository.SaveChangesAsync(ct);

            afterCommit.Add(() =>
            {
                connections.Revoke(
                    userId,
                    exceptSession: currentSessionId);

                return Task.CompletedTask;
            });

            return true;
        }, ct);
    }

    // =====================================================
    // REACTIVATE ACCOUNT
    // =====================================================

    public Task<bool> ReactivateAsync(
        ReactivateRequest input,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(async _ =>
        {
            var normalized = input.Identifier
                .Trim()
                .ToUpperInvariant();

            var user =
                await repository
                    .GetUserByIdentifierForUpdateAsync(
                        normalized,
                        ct);

            if (user is null ||
                user.EmailVerifiedAt is null ||
                user.DeactivatedAt is null ||
                await IsSuspendedAsync(user.Id, ct))
            {
                return false;
            }

            var result =
                _hasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    input.Password);

            if (result ==
                PasswordVerificationResult.Failed)
            {
                return false;
            }

            user.DeactivatedAt = null;

            await repository.SaveChangesAsync(ct);

            return true;
        }, ct);
    }

    // =====================================================
    // DEACTIVATE ACCOUNT
    // =====================================================

    public Task<bool> DeactivateAsync(
        Guid userId,
        string password,
        CancellationToken ct)
    {
        return AtomicAsync<bool>(async afterCommit =>
        {
            var user =
                await repository.GetUserForUpdateAsync(
                    userId,
                    ct);

            if (user is null)
                return false;

            var result =
                _hasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    password);

            if (result ==
                PasswordVerificationResult.Failed)
            {
                return false;
            }

            user.DeactivatedAt = Now;

            await repository.RevokeAllSessionsAsync(
                userId,
                Now,
                ct);

            await repository.SaveChangesAsync(ct);

            afterCommit.Add(() =>
            {
                connections.Revoke(userId);
                return Task.CompletedTask;
            });

            return true;
        }, ct);
    }

    // =====================================================
    // ACTIVE SESSIONS
    // =====================================================

    public Task<List<SessionResponse>> SessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct)
    {
        return repository.GetActiveSessionsAsync(
            userId,
            currentSessionId,
            ct);
    }

    // =====================================================
    // ACCOUNT SUSPENSION
    // =====================================================

    public Task<bool> IsSuspendedAsync(
        Guid userId,
        CancellationToken ct)
    {
        return repository.IsSuspendedAsync(
            userId,
            Now,
            ct);
    }

    // =====================================================
    // TRANSACTION + AFTER COMMIT
    // =====================================================

    private async Task<T> AtomicAsync<T>(
        Func<List<Func<Task>>, Task<T>> operation,
        CancellationToken ct)
    {
        var afterCommit = new List<Func<Task>>();

        var result =
            await repository.ExecuteAtomicAsync(
                async () =>
                {
                    // Retry must discard side effects
                    // recorded by failed attempts.
                    afterCommit.Clear();

                    return await operation(afterCommit);
                },
                ct);

        // Only execute after transaction commit.
        foreach (var action in afterCommit)
        {
            await action();
        }

        return result;
    }

    // =====================================================
    // JWT ACCESS TOKEN
    // =====================================================

    private AccessResponse IssueAccess(
        User user,
        Guid sessionId)
    {
        var key = configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(key) ||
            Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be configured in local secret storage.");
        }

        var expires = Now.AddMinutes(15);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                "sid",
                sessionId.ToString()),

            new Claim(
                ClaimTypes.Role,
                user.SystemRole)
        };

        var jwt = new JwtSecurityToken(
            issuer: "MeChat.Api",
            audience: "MeChat.Frontend",
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AccessResponse(
            accessToken,
            expires,
            user.Id,
            user.Username);
    }

    // =====================================================
    // TOKEN GENERATION
    // =====================================================

    private static string NewToken()
    {
        return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // =====================================================
    // TOKEN HASH
    // =====================================================

    private static string Hash(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token)));
    }
}

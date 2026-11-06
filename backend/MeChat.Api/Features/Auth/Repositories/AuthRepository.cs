
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Auth;
using MeChat.Api.Features.Auth.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Auth.Repositories;

public sealed class AuthRepository(AppDbContext db) : IAuthRepository
{
    // =====================================================
    // TRANSACTION / UNIT OF WORK
    // =====================================================

    public async Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var transaction =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        ct);

                var result = await operation();

                await transaction.CommitAsync(ct);

                return result;
            }
            catch (Exception error)
                when (attempt < 2 && IsRetryable(error))
            {
                db.ChangeTracker.Clear();

                await Task.Delay(
                    30 * (attempt + 1),
                    ct);
            }
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    private static bool IsRetryable(Exception error)
    {
        if (error is DbUpdateConcurrencyException)
            return true;

        if (error is SqlException sql &&
            sql.Number is 1205 or 2601 or 2627)
            return true;

        return error.InnerException is not null &&
               IsRetryable(error.InnerException);
    }

    // =====================================================
    // USERS
    // =====================================================

    public Task<bool> UserExistsAsync(
        string normalizedEmail,
        string normalizedUsername,
        CancellationToken ct)
    {
        return db.Users.AnyAsync(
            user =>
                user.NormalizedEmail == normalizedEmail ||
                user.NormalizedUsername == normalizedUsername,
            ct);
    }

    public Task<User?> GetUserByEmailAsync(
        string normalizedEmail,
        CancellationToken ct)
    {
        return db.Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail,
            ct);
    }

    public Task<User?> GetUserByIdentifierForUpdateAsync(
        string normalizedIdentifier,
        CancellationToken ct)
    {
        return db.Users
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [Users] WITH (UPDLOCK, HOLDLOCK)
                WHERE [NormalizedEmail] = {normalizedIdentifier}
                   OR [NormalizedUsername] = {normalizedIdentifier}
                """)
            .SingleOrDefaultAsync(ct);
    }

    public Task<User?> GetUserForUpdateAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [Users] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {userId}
                """)
            .SingleOrDefaultAsync(ct);
    }

    public void AddUser(User user)
    {
        db.Users.Add(user);
    }

    public void AddPrivacySetting(UserPrivacySetting setting)
    {
        db.UserPrivacySettings.Add(setting);
    }

    // =====================================================
    // AUTH TOKENS
    // =====================================================

    public void AddAuthToken(AuthToken token)
    {
        db.AuthTokens.Add(token);
    }

    public Task<AuthToken?> GetAuthTokenForUpdateAsync(
        string tokenHash,
        string tokenType,
        CancellationToken ct)
    {
        return db.AuthTokens
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [AuthTokens] WITH (UPDLOCK, HOLDLOCK)
                WHERE [TokenHash] = {tokenHash}
                  AND [Type] = {tokenType}
                """)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<bool> TryConsumeAuthTokenAsync(
        Guid tokenId,
        DateTime now,
        CancellationToken ct)
    {
        var changed = await db.AuthTokens
            .Where(token =>
                token.Id == tokenId &&
                token.UsedAt == null &&
                token.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    token => token.UsedAt,
                    now),
                ct);

        return changed == 1;
    }

    // =====================================================
    // REFRESH TOKENS
    // =====================================================

    public void AddRefreshToken(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(
        string tokenHash,
        CancellationToken ct)
    {
        return db.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            ct);
    }

    public async Task<bool> TryRevokeRefreshTokenAsync(
        Guid tokenId,
        DateTime now,
        CancellationToken ct)
    {
        var changed = await db.RefreshTokens
            .Where(token =>
                token.Id == tokenId &&
                token.RevokedAt == null &&
                token.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    token => token.RevokedAt,
                    now),
                ct);

        return changed == 1;
    }

    // =====================================================
    // SESSIONS
    // =====================================================

    public void AddSession(UserSession session)
    {
        db.UserSessions.Add(session);
    }

    public Task<UserSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken ct)
    {
        return db.UserSessions.FindAsync(
            [sessionId],
            ct).AsTask();
    }

    public Task<int> RevokeAllSessionsAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.UserSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    now),
                ct);
    }

    public Task<int> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        DateTime now,
        CancellationToken ct)
    {
        return db.UserSessions
            .Where(session =>
                session.Id == sessionId &&
                session.UserId == userId &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    now),
                ct);
    }

    public Task<int> RevokeOtherSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        DateTime now,
        CancellationToken ct)
    {
        return db.UserSessions
            .Where(session =>
                session.UserId == userId &&
                session.Id != currentSessionId &&
                session.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    session => session.RevokedAt,
                    now),
                ct);
    }

    public Task<List<SessionResponse>> GetActiveSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct)
    {
        return db.UserSessions
            .AsNoTracking()
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAt == null)
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => new SessionResponse(
                session.Id,
                session.DeviceName,
                session.CreatedAt,
                session.LastActivityAt,
                session.Id == currentSessionId))
            .ToListAsync(ct);
    }

    // =====================================================
    // ACCOUNT SUSPENSION
    // =====================================================

    public Task<bool> IsSuspendedAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.AccountSuspensions.AnyAsync(
            suspension =>
                suspension.UserId == userId &&
                suspension.Status == "Active" &&
                suspension.RevokedAt == null &&
                (
                    suspension.ExpiresAt == null ||
                    suspension.ExpiresAt > now
                ),
            ct);
    }
}

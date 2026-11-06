
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Users.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Users.Repositories;

public sealed class UserRepository(
    AppDbContext db) : IUserRepository
{
    // =====================================================
    // TRANSACTION + RETRY
    // =====================================================

    public async Task<bool> ExecuteAtomicAsync(
        Func<Task<bool>> operation,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var transaction =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        ct);

                var success = await operation();

                // Failed business validation must not
                // leave tracked changes to be saved later.
                if (!success)
                {
                    await transaction.RollbackAsync(ct);
                    db.ChangeTracker.Clear();

                    return false;
                }

                await transaction.CommitAsync(ct);

                return true;
            }
            catch (Exception error)
                when (attempt < 2 && IsRetryable(error))
            {
                // A failed transaction must not carry
                // tracked entities into its next attempt.
                db.ChangeTracker.Clear();

                await Task.Delay(
                    30 * (attempt + 1),
                    ct);
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }

        throw new InvalidOperationException(
            "User transaction retry limit reached.");
    }

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // GET PRIVATE PROFILE
    // =====================================================

    public Task<PrivateProfileDto?> GetPrivateProfileAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new PrivateProfileDto(
                user.Id,
                user.Email,
                user.Username,
                user.DisplayName,
                user.Bio,
                user.AvatarFileId,
                user.SystemRole,
                user.CreatedAt,
                user.LastUsernameChangedAt))
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // GET PUBLIC PROFILE
    // =====================================================

    public Task<PublicProfileDto?> GetPublicProfileAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId &&
                user.EmailVerifiedAt != null &&
                user.DeactivatedAt == null &&

                !db.AccountSuspensions.Any(suspension =>
                    suspension.UserId == user.Id &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (
                        suspension.ExpiresAt == null ||
                        suspension.ExpiresAt > now
                    )))
            .Select(user => new PublicProfileDto(
                user.Id,
                user.Username,
                user.DisplayName,
                user.Bio,
                user.AvatarFileId))
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // GET USER FOR UPDATE
    // =====================================================

    public async Task<User?> GetUserAsync(
        Guid userId,
        CancellationToken ct)
    {
        return await db.Users.FindAsync(
            [userId],
            ct);
    }

    // =====================================================
    // GET USER WITH SQL SERVER LOCK
    // =====================================================

    public Task<User?> GetUserForUpdateAsync(
        Guid userId,
        CancellationToken ct)
    {
        // Execute within ExecuteAtomicAsync.
        // Prevent concurrent changes to this User row.
        //
        // FromSqlInterpolated parameterizes userId.

        return db.Users
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [Users] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {userId}
                """)
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // CHECK USERNAME DUPLICATE
    // =====================================================

    public Task<bool> UsernameExistsAsync(
        string normalizedUsername,
        Guid excludeUserId,
        CancellationToken ct)
    {
        return db.Users.AnyAsync(
            user =>
                user.NormalizedUsername == normalizedUsername &&
                user.Id != excludeUserId,
            ct);
    }

    // =====================================================
    // CHECK EMAIL DUPLICATE
    // =====================================================

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid excludeUserId,
        CancellationToken ct)
    {
        return db.Users.AnyAsync(
            user =>
                user.NormalizedEmail == normalizedEmail &&
                user.Id != excludeUserId,
            ct);
    }

    // =====================================================
    // ADD AUTH TOKEN
    // =====================================================

    public void AddAuthToken(AuthToken token)
    {
        db.AuthTokens.Add(token);
    }

    // =====================================================
    // EMAIL CHANGE TOKEN WITH UPDATE LOCK
    // =====================================================

    public Task<AuthToken?> GetEmailChangeTokenForUpdateAsync(
        string tokenHash,
        Guid userId,
        CancellationToken ct)
    {
        // Keep the token lock from the original logic.
        // Additional predicates are translated by EF Core.

        return db.AuthTokens
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [AuthTokens] WITH (UPDLOCK, HOLDLOCK)
                WHERE [TokenHash] = {tokenHash}
                """)
            .SingleOrDefaultAsync(
                token =>
                    token.UserId == userId &&
                    token.Type == "EmailChange",
                ct);
    }

    // =====================================================
    // CONSUME EMAIL CHANGE TOKEN ATOMICALLY
    // =====================================================

    public async Task<bool> TryConsumeTokenAsync(
        Guid tokenId,
        DateTime now,
        CancellationToken ct)
    {
        var claimed = await db.AuthTokens
            .Where(token =>
                token.Id == tokenId &&
                token.UsedAt == null &&
                token.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    token => token.UsedAt,
                    now),
                ct);

        return claimed == 1;
    }

    // =====================================================
    // GET PRIVACY
    // =====================================================

    public Task<PrivacyDto?> GetPrivacyAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.UserPrivacySettings
            .AsNoTracking()
            .Where(setting =>
                setting.UserId == userId)
            .Select(setting => new PrivacyDto(
                setting.ShowOnline,
                setting.ShowLastSeen,
                setting.AllowStrangerMessages,
                setting.AllowGroupInvites))
            .SingleOrDefaultAsync(ct);
    }

    // =====================================================
    // GET PRIVACY SETTING FOR UPDATE
    // =====================================================

    public async Task<UserPrivacySetting?>
        GetPrivacySettingAsync(
            Guid userId,
            CancellationToken ct)
    {
        return await db.UserPrivacySettings.FindAsync(
            [userId],
            ct);
    }

    // =====================================================
    // SEARCH USERS
    // =====================================================

    public Task<List<PublicProfileDto>> SearchUsersAsync(
        string normalizedQuery,
        string originalQuery,
        DateTime now,
        int offset,
        int take,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .Where(user =>
                user.EmailVerifiedAt != null &&
                user.DeactivatedAt == null &&

                (
                    user.NormalizedUsername.Contains(
                        normalizedQuery) ||

                    user.DisplayName.Contains(
                        originalQuery)
                ) &&

                !db.AccountSuspensions.Any(suspension =>
                    suspension.UserId == user.Id &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (
                        suspension.ExpiresAt == null ||
                        suspension.ExpiresAt > now
                    )))
            .OrderBy(user =>
                user.NormalizedUsername)
            .ThenBy(user => user.Id)
            .Skip(offset)
            .Take(take)
            .Select(user => new PublicProfileDto(
                user.Id,
                user.Username,
                user.DisplayName,
                user.Bio,
                user.AvatarFileId))
            .ToListAsync(ct);
    }

    // =====================================================
    // RETRYABLE DATABASE ERRORS
    // =====================================================

    private static bool IsRetryable(Exception error)
    {
        for (
            Exception? current = error;
            current is not null;
            current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException)
            {
                return true;
            }

            if (current is SqlException sql &&
                sql.Number is 1205 or 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}

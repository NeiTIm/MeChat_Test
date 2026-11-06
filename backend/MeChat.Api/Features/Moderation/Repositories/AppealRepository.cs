
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation.Repositories;

public sealed class AppealRepository(
    AppDbContext db) : IAppealRepository
{
    // =====================================================
    // FIND USER BY EMAIL OR USERNAME
    // =====================================================

    public Task<User?> FindUserByIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user =>
                    user.NormalizedEmail == normalizedIdentifier ||
                    user.NormalizedUsername == normalizedIdentifier,
                ct);
    }

    // =====================================================
    // CHECK ACTIVE SUSPENSION
    // =====================================================

    public Task<bool> HasActiveSuspensionAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        return db.AccountSuspensions
            .AsNoTracking()
            .AnyAsync(
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

    // =====================================================
    // GET LATEST ACTIVE SUSPENSION WITH UPDATE LOCK
    // =====================================================

    public Task<AccountSuspension?> GetLatestActiveSuspensionAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct)
    {
        // Must be called within ExecuteAtomicAsync.
        // UPDLOCK and HOLDLOCK serialize competing requests
        // for appeals associated with the same suspension.

        return db.AccountSuspensions
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [AccountSuspensions] WITH (UPDLOCK, HOLDLOCK)
                WHERE [UserId] = {userId}
                """)
            .Where(suspension =>
                suspension.Status == "Active" &&
                suspension.RevokedAt == null &&
                (
                    suspension.ExpiresAt == null ||
                    suspension.ExpiresAt > now
                ))
            .OrderByDescending(
                suspension => suspension.StartedAt)
            .ThenByDescending(
                suspension => suspension.Id)
            .FirstOrDefaultAsync(ct);
    }

    // =====================================================
    // CHECK PENDING APPEAL
    // =====================================================

    public Task<bool> HasPendingAppealAsync(
        Guid suspensionId,
        CancellationToken ct)
    {
        return db.AccountAppeals
            .AnyAsync(
                appeal =>
                    appeal.SuspensionId == suspensionId &&
                    appeal.Status == "Pending",
                ct);
    }

    // =====================================================
    // ADD APPEAL
    // =====================================================

    public void AddAppeal(
        AccountAppeal appeal)
    {
        db.AccountAppeals.Add(appeal);
    }

    // =====================================================
    // SAVE DATABASE CHANGES
    // =====================================================

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // GET MY APPEAL HISTORY
    // =====================================================

    public Task<List<AppealDto>> GetUserAppealsAsync(
        Guid userId,
        CancellationToken ct)
    {
        return db.AccountAppeals
            .AsNoTracking()
            .Where(appeal =>
                appeal.UserId == userId)
            .OrderByDescending(
                appeal => appeal.CreatedAt)
            .Select(appeal => new AppealDto(
                appeal.Id,
                appeal.SuspensionId,
                appeal.Reason,
                appeal.Status,
                appeal.CreatedAt,
                appeal.ReviewedAt,
                appeal.ReviewNote))
            .ToListAsync(ct);
    }

    // =====================================================
    // SERIALIZABLE TRANSACTION + RETRY
    // =====================================================

    public async Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool> shouldCommit,
        CancellationToken ct)
    {
        const int maxAttempts = 3;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await using var transaction =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        ct);

                var result = await operation();

                if (!shouldCommit(result))
                {
                    await transaction.RollbackAsync(ct);

                    db.ChangeTracker.Clear();

                    return result;
                }

                await transaction.CommitAsync(ct);

                return result;
            }
            catch (Exception error)
                when (
                    attempt < maxAttempts - 1 &&
                    IsRetryable(error))
            {
                db.ChangeTracker.Clear();

                await Task.Delay(
                    40 * (attempt + 1),
                    ct);
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }

        throw new InvalidOperationException(
            "Appeal transaction retry limit reached.");
    }

    // =====================================================
    // RETRYABLE SQL SERVER ERRORS
    // =====================================================

    private static bool IsRetryable(
        Exception error)
    {
        for (
            Exception? current = error;
            current is not null;
            current = current.InnerException)
        {
            if (current is SqlException sql &&
                sql.Number is 1205 or 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}

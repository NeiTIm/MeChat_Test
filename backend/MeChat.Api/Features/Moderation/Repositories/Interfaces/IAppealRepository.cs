
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation.Repositories.Interfaces;

public interface IAppealRepository
{
    // =====================================================
    // AUTHENTICATION
    // =====================================================

    Task<User?> FindUserByIdentifierAsync(
        string normalizedIdentifier,
        CancellationToken ct);

    // =====================================================
    // ACCOUNT SUSPENSIONS
    // =====================================================

    Task<bool> HasActiveSuspensionAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    Task<AccountSuspension?> GetLatestActiveSuspensionAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // APPEAL VALIDATION
    // =====================================================

    Task<bool> HasPendingAppealAsync(
        Guid suspensionId,
        CancellationToken ct);

    // =====================================================
    // APPEAL PERSISTENCE
    // =====================================================

    void AddAppeal(AccountAppeal appeal);

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // APPEAL HISTORY
    // =====================================================

    Task<List<AppealDto>> GetUserAppealsAsync(
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // ATOMIC CREATE
    // =====================================================

    Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool> shouldCommit,
        CancellationToken ct);
}

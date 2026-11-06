
using MeChat.Api.Features.Auth;

namespace MeChat.Api.Features.Auth.Repositories.Interfaces;

public interface IAuthRepository
{
    // =====================================================
    // TRANSACTION / UNIT OF WORK
    // =====================================================

    Task<T> ExecuteAtomicAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);

    // =====================================================
    // USERS
    // =====================================================

    Task<bool> UserExistsAsync(
        string normalizedEmail,
        string normalizedUsername,
        CancellationToken ct);

    Task<User?> GetUserByEmailAsync(
        string normalizedEmail,
        CancellationToken ct);

    Task<User?> GetUserByIdentifierForUpdateAsync(
        string normalizedIdentifier,
        CancellationToken ct);

    Task<User?> GetUserForUpdateAsync(
        Guid userId,
        CancellationToken ct);

    void AddUser(User user);

    void AddPrivacySetting(UserPrivacySetting setting);

    // =====================================================
    // AUTH TOKENS
    // =====================================================

    void AddAuthToken(AuthToken token);

    Task<AuthToken?> GetAuthTokenForUpdateAsync(
        string tokenHash,
        string tokenType,
        CancellationToken ct);

    Task<bool> TryConsumeAuthTokenAsync(
        Guid tokenId,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // REFRESH TOKENS
    // =====================================================

    void AddRefreshToken(RefreshToken token);

    Task<RefreshToken?> GetRefreshTokenAsync(
        string tokenHash,
        CancellationToken ct);

    Task<bool> TryRevokeRefreshTokenAsync(
        Guid tokenId,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // SESSIONS
    // =====================================================

    void AddSession(UserSession session);

    Task<UserSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken ct);

    Task<int> RevokeAllSessionsAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    Task<int> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        DateTime now,
        CancellationToken ct);

    Task<int> RevokeOtherSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        DateTime now,
        CancellationToken ct);

    Task<List<SessionResponse>> GetActiveSessionsAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct);

    // =====================================================
    // ACCOUNT SUSPENSION
    // =====================================================

    Task<bool> IsSuspendedAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);
}


using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Users.Repositories.Interfaces;

public interface IUserRepository
{
    // =====================================================
    // TRANSACTION / PERSISTENCE
    // =====================================================

    Task<bool> ExecuteAtomicAsync(
        Func<Task<bool>> operation,
        CancellationToken ct);

    Task<int> SaveChangesAsync(
        CancellationToken ct);

    // =====================================================
    // USER PROFILE
    // =====================================================

    Task<PrivateProfileDto?> GetPrivateProfileAsync(
        Guid userId,
        CancellationToken ct);

    Task<PublicProfileDto?> GetPublicProfileAsync(
        Guid userId,
        DateTime now,
        CancellationToken ct);

    Task<User?> GetUserAsync(
        Guid userId,
        CancellationToken ct);

    Task<User?> GetUserForUpdateAsync(
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // USERNAME
    // =====================================================

    Task<bool> UsernameExistsAsync(
        string normalizedUsername,
        Guid excludeUserId,
        CancellationToken ct);

    // =====================================================
    // EMAIL
    // =====================================================

    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid excludeUserId,
        CancellationToken ct);

    // =====================================================
    // AUTH TOKENS
    // =====================================================

    void AddAuthToken(AuthToken token);

    Task<AuthToken?> GetEmailChangeTokenForUpdateAsync(
        string tokenHash,
        Guid userId,
        CancellationToken ct);

    Task<bool> TryConsumeTokenAsync(
        Guid tokenId,
        DateTime now,
        CancellationToken ct);

    // =====================================================
    // PRIVACY
    // =====================================================

    Task<PrivacyDto?> GetPrivacyAsync(
        Guid userId,
        CancellationToken ct);

    Task<UserPrivacySetting?> GetPrivacySettingAsync(
        Guid userId,
        CancellationToken ct);

    // =====================================================
    // USER SEARCH
    // =====================================================

    Task<List<PublicProfileDto>> SearchUsersAsync(
        string normalizedQuery,
        string originalQuery,
        DateTime now,
        int offset,
        int take,
        CancellationToken ct);
}


using System.Security.Cryptography;
using System.Text;
using MeChat.Api.Features.Auth;
using MeChat.Api.Features.Users.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Users;

public sealed class UserService(
    IUserRepository repository,
    IAuthMailSender mail,
    TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // GET MY PROFILE
    // =====================================================

    public Task<PrivateProfileDto?> GetMeAsync(
        Guid id,
        CancellationToken ct)
    {
        return repository.GetPrivateProfileAsync(
            id,
            ct);
    }

    // =====================================================
    // GET PUBLIC PROFILE
    // =====================================================

    public Task<PublicProfileDto?> GetPublicAsync(
        Guid id,
        CancellationToken ct)
    {
        return repository.GetPublicProfileAsync(
            id,
            Now,
            ct);
    }

    // =====================================================
    // UPDATE MY PROFILE
    // =====================================================

    public async Task<PrivateProfileDto?> UpdateProfileAsync(
        Guid id,
        UpdateProfileRequest input,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE DISPLAY NAME
        // =================================================

        if (string.IsNullOrWhiteSpace(input.DisplayName))
        {
            return null;
        }

        // =================================================
        // GET USER
        // =================================================

        var user = await repository.GetUserAsync(
            id,
            ct);

        if (user is null)
        {
            return null;
        }

        // =================================================
        // UPDATE PROFILE
        // =================================================

        user.DisplayName = input.DisplayName.Trim();
        user.Bio = input.Bio?.Trim();

        // =================================================
        // SAVE CHANGES
        // =================================================

        await repository.SaveChangesAsync(ct);

        // =================================================
        // RETURN UPDATED PROFILE
        // =================================================

        return await GetMeAsync(id, ct);
    }

    // =====================================================
    // CHANGE USERNAME
    // =====================================================

    public Task<bool> ChangeUsernameAsync(
        Guid id,
        string value,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(false);
        }

        var username = value.Trim();

        // =================================================
        // VALIDATE USERNAME
        // =================================================

        if (username.Length is < 3 or > 50)
        {
            return Task.FromResult(false);
        }

        return repository.ExecuteAtomicAsync(
            async () =>
            {
                // =================================================
                // LOCK USER
                // =================================================

                var user =
                    await repository.GetUserForUpdateAsync(
                        id,
                        ct);

                if (user is null)
                {
                    return false;
                }

                var now = Now;

                // =================================================
                // 30-DAY USERNAME CHANGE LIMIT
                // =================================================

                if (user.LastUsernameChangedAt >
                    now.AddDays(-30))
                {
                    return false;
                }

                // =================================================
                // NORMALIZE USERNAME
                // =================================================

                var normalized =
                    username.ToUpperInvariant();

                // =================================================
                // DUPLICATE CHECK
                // =================================================

                var exists =
                    await repository.UsernameExistsAsync(
                        normalized,
                        id,
                        ct);

                if (exists)
                {
                    return false;
                }

                // =================================================
                // UPDATE USERNAME
                // =================================================

                user.Username = username;
                user.NormalizedUsername = normalized;
                user.LastUsernameChangedAt = now;

                await repository.SaveChangesAsync(ct);

                return true;
            },
            ct);
    }

    // =====================================================
    // REQUEST EMAIL CHANGE
    // =====================================================

    public async Task<bool> RequestEmailChangeAsync(
        Guid id,
        string value,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE EMAIL
        // =================================================

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var email = value.Trim();

        if (email.Length > 320)
        {
            return false;
        }

        // =================================================
        // CHECK USER
        // =================================================

        var user =
            await repository.GetUserAsync(
                id,
                ct);

        if (user is null)
        {
            return false;
        }

        // =================================================
        // CHECK DUPLICATE EMAIL
        // =================================================

        var normalizedEmail =
            email.ToUpperInvariant();

        var exists =
            await repository.EmailExistsAsync(
                normalizedEmail,
                id,
                ct);

        if (exists)
        {
            return false;
        }

        // =================================================
        // GENERATE SECURE TOKEN
        // =================================================

        // The token is sent to the new email address.
        // Only its SHA-256 hash is saved in SQL Server.

        var raw = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var now = Now;

        // =================================================
        // CREATE AUTH TOKEN
        // =================================================

        var token = new AuthToken
        {
            Id = Guid.NewGuid(),
            UserId = id,
            Type = "EmailChange",
            TokenHash = Hash(raw),
            PendingEmail = email,
            ExpiresAt = now.AddHours(24)
        };

        repository.AddAuthToken(token);

        // =================================================
        // SAVE BEFORE SENDING EMAIL
        // =================================================

        await repository.SaveChangesAsync(ct);

        // =================================================
        // SEND EMAIL
        // =================================================

        await mail.SendTokenAsync(
            email,
            raw,
            "email-change",
            ct);

        return true;
    }

    // =====================================================
    // CONFIRM EMAIL CHANGE
    // =====================================================

    public Task<bool> ConfirmEmailChangeAsync(
        string raw,
        Guid userId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Task.FromResult(false);
        }

        var hash = Hash(raw);

        return repository.ExecuteAtomicAsync(
            async () =>
            {
                // =================================================
                // LOCK EMAIL CHANGE TOKEN
                // =================================================

                var token =
                    await repository
                        .GetEmailChangeTokenForUpdateAsync(
                            hash,
                            userId,
                            ct);

                var now = Now;

                // =================================================
                // VALIDATE TOKEN
                // =================================================

                if (token is null ||
                    token.UsedAt is not null ||
                    token.ExpiresAt <= now ||
                    token.PendingEmail is null)
                {
                    return false;
                }

                // =================================================
                // VALIDATE TARGET EMAIL
                // =================================================

                var email = token.PendingEmail;
                var normalizedEmail =
                    email.ToUpperInvariant();

                var exists =
                    await repository.EmailExistsAsync(
                        normalizedEmail,
                        userId,
                        ct);

                if (exists)
                {
                    return false;
                }

                // =================================================
                // CLAIM TOKEN ATOMICALLY
                // =================================================

                var claimed =
                    await repository.TryConsumeTokenAsync(
                        token.Id,
                        now,
                        ct);

                if (!claimed)
                {
                    return false;
                }

                // =================================================
                // LOCK USER
                // =================================================

                var user =
                    await repository.GetUserForUpdateAsync(
                        userId,
                        ct);

                if (user is null)
                {
                    return false;
                }

                // =================================================
                // UPDATE EMAIL
                // =================================================

                user.Email = email;
                user.NormalizedEmail = normalizedEmail;

                await repository.SaveChangesAsync(ct);

                return true;
            },
            ct);
    }

    // =====================================================
    // GET PRIVACY SETTINGS
    // =====================================================

    public Task<PrivacyDto?> GetPrivacyAsync(
        Guid userId,
        CancellationToken ct)
    {
        return repository.GetPrivacyAsync(
            userId,
            ct);
    }

    // =====================================================
    // UPDATE PRIVACY SETTINGS
    // =====================================================

    public async Task<PrivacyDto?> UpdatePrivacyAsync(
        Guid userId,
        PrivacyDto input,
        CancellationToken ct)
    {
        // =================================================
        // GET SETTINGS
        // =================================================

        var setting =
            await repository.GetPrivacySettingAsync(
                userId,
                ct);

        if (setting is null)
        {
            return null;
        }

        // =================================================
        // ONLINE VISIBILITY
        // =================================================

        setting.ShowOnline = input.ShowOnline;

        // =================================================
        // LAST SEEN VISIBILITY
        // =================================================

        setting.ShowLastSeen = input.ShowLastSeen;

        // =================================================
        // STRANGER MESSAGE PERMISSION
        // =================================================

        setting.AllowStrangerMessages =
            input.AllowStrangerMessages;

        // =================================================
        // GROUP INVITATION PERMISSION
        // =================================================

        setting.AllowGroupInvites =
            input.AllowGroupInvites;

        // =================================================
        // SAVE
        // =================================================

        await repository.SaveChangesAsync(ct);

        return input;
    }

    // =====================================================
    // SEARCH USERS
    // =====================================================

    public async Task<SearchPageDto> SearchAsync(
        string query,
        string? cursor,
        int limit,
        CancellationToken ct)
    {
        // =================================================
        // NORMALIZE QUERY
        // =================================================

        var search = query?.Trim() ?? "";

        var normalized =
            search.ToUpperInvariant();

        // =================================================
        // MINIMUM SEARCH LENGTH
        // =================================================

        if (normalized.Length < 2)
        {
            return new SearchPageDto(
                [],
                null,
                false);
        }

        // =================================================
        // PAGINATION LIMIT
        // =================================================

        var count = Math.Clamp(
            limit,
            1,
            30);

        // =================================================
        // OFFSET CURSOR
        // =================================================

        var offset =
            int.TryParse(cursor, out var parsed) &&
            parsed >= 0
                ? Math.Min(parsed, 10000)
                : 0;

        // =================================================
        // QUERY DATABASE
        // =================================================

        // Get one extra item to determine HasMore.

        var users =
            await repository.SearchUsersAsync(
                normalized,
                search,
                Now,
                offset,
                count + 1,
                ct);

        // =================================================
        // PAGINATION RESULT
        // =================================================

        var hasMore =
            users.Count > count;

        var nextCursor = hasMore
            ? (offset + count).ToString()
            : null;

        return new SearchPageDto(
            users.Take(count).ToList(),
            nextCursor,
            hasMore);
    }

    // =====================================================
    // SHA-256 TOKEN HASH
    // =====================================================

    private static string Hash(string raw)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(raw)));
    }
}

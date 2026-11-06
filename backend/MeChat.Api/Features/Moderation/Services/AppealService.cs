
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MeChat.Api.Features.Moderation.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Moderation;

public sealed class AppealService(
    IAppealRepository repository,
    IConfiguration configuration,
    TimeProvider clock)
{
    private readonly PasswordHasher<User> hasher = new();

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // AUTHENTICATE SUSPENDED USER
    // =====================================================

    public async Task<AppealAccessDto?> AuthenticateAsync(
        AppealLoginRequest input,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE IDENTIFIER
        // =================================================

        var identifier =
            input.Identifier?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(identifier) ||
            string.IsNullOrEmpty(input.Password))
        {
            return null;
        }

        // =================================================
        // FIND USER
        // =================================================

        var user =
            await repository.FindUserByIdentifierAsync(
                identifier,
                ct);

        if (user is null)
            return null;

        var now = Now;

        // =================================================
        // ACCOUNT VALIDATION
        // =================================================

        if (user.EmailVerifiedAt is null ||
            user.DeactivatedAt is not null ||
            user.LockoutUntil > now)
        {
            return null;
        }

        // =================================================
        // VERIFY PASSWORD
        // =================================================

        var verification =
            hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                input.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // =================================================
        // REQUIRE ACTIVE SUSPENSION
        // =================================================

        // This login flow is only for users who are
        // currently suspended and need to submit an appeal.

        var suspended =
            await repository.HasActiveSuspensionAsync(
                user.Id,
                now,
                ct);

        if (!suspended)
            return null;

        // =================================================
        // JWT SIGNING KEY
        // =================================================

        var key = configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(key) ||
            Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException(
                "JWT key unavailable.");
        }

        // =================================================
        // APPEAL-ONLY TOKEN
        // =================================================

        var expires = Now.AddMinutes(10);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                "scope",
                "appeal")
        };

        // =================================================
        // GENERATE JWT
        // =================================================

        var jwt = new JwtSecurityToken(
            issuer: "MeChat.Api",
            audience: "MeChat.Frontend",
            claims: claims,
            expires: expires,
            signingCredentials:
                new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256));

        var accessToken =
            new JwtSecurityTokenHandler()
                .WriteToken(jwt);

        // =================================================
        // RESPONSE
        // =================================================

        return new AppealAccessDto(
            accessToken,
            expires);
    }

    // =====================================================
    // CREATE ACCOUNT APPEAL
    // =====================================================

    public Task<AppealDto?> CreateAsync(
        Guid actor,
        string reason,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE REASON
        // =================================================

        var normalizedReason = reason?.Trim() ?? "";

        if (normalizedReason.Length is < 10 or > 1000)
        {
            return Task.FromResult<AppealDto?>(null);
        }

        // =================================================
        // ATOMIC APPEAL CREATION
        // =================================================

        return repository.ExecuteAtomicAsync(
            async () =>
            {
                var now = Now;

                // =================================================
                // GET ACTIVE SUSPENSION
                // =================================================

                var suspension =
                    await repository
                        .GetLatestActiveSuspensionAsync(
                            actor,
                            now,
                            ct);

                if (suspension is null)
                    return null;

                // =================================================
                // CHECK EXISTING PENDING APPEAL
                // =================================================

                var alreadyPending =
                    await repository.HasPendingAppealAsync(
                        suspension.Id,
                        ct);

                if (alreadyPending)
                    return null;

                // =================================================
                // CREATE APPEAL
                // =================================================

                var appeal = new AccountAppeal
                {
                    Id = Guid.NewGuid(),
                    SuspensionId = suspension.Id,
                    UserId = actor,
                    Reason = normalizedReason,
                    Status = "Pending",
                    CreatedAt = now
                };

                repository.AddAppeal(appeal);

                // =================================================
                // SAVE CHANGES
                // =================================================

                await repository.SaveChangesAsync(ct);

                // =================================================
                // RESPONSE
                // =================================================

                return Map(appeal);
            },
            result => result is not null,
            ct);
    }

    // =====================================================
    // GET MY APPEALS
    // =====================================================

    public Task<List<AppealDto>> MineAsync(
        Guid actor,
        CancellationToken ct)
    {
        return repository.GetUserAppealsAsync(
            actor,
            ct);
    }

    // =====================================================
    // MAP ENTITY TO DTO
    // =====================================================

    private static AppealDto Map(
        AccountAppeal appeal)
    {
        return new AppealDto(
            appeal.Id,
            appeal.SuspensionId,
            appeal.Reason,
            appeal.Status,
            appeal.CreatedAt,
            appeal.ReviewedAt,
            appeal.ReviewNote);
    }
}

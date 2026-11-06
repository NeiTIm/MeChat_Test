
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Configurations;

/// <summary>
/// Handles JWT token retrieval and validates
/// account/session eligibility for MeChat.
/// </summary>
public sealed class JwtSessionValidationEvents(
    AppDbContext db,
    TimeProvider clock,
    ILogger<JwtSessionValidationEvents> logger)
    : JwtBearerEvents
{
    // =====================================================
    // MESSAGE RECEIVED
    // =====================================================

    public override Task MessageReceived(
        MessageReceivedContext context)
    {
        // =================================================
        // SIGNALR ACCESS TOKEN
        // =================================================

        // SignalR browser clients may send access_token
        // as a query parameter during WebSocket setup.
        //
        // Only accept this for the existing chat hub path.

        if (context.Request.Path.StartsWithSegments(
                "/hubs/chat") &&
            context.Request.Query.TryGetValue(
                "access_token",
                out var token))
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    }

    // =====================================================
    // TOKEN VALIDATED
    // =====================================================

    public override async Task TokenValidated(
        TokenValidatedContext context)
    {
        var cancellationToken =
            context.HttpContext.RequestAborted;

        // =================================================
        // EXTRACT USER ID
        // =================================================

        var userIdText =
            context.Principal?.FindFirstValue(
                JwtRegisteredClaimNames.Sub)
            ?? context.Principal?.FindFirstValue(
                ClaimTypes.NameIdentifier);

        // =================================================
        // SPECIAL APPEAL TOKEN
        // =================================================

        if (context.Principal?.FindFirstValue("scope")
            == "appeal")
        {
            await ValidateAppealTokenAsync(
                context,
                userIdText,
                cancellationToken);

            return;
        }

        // =================================================
        // NORMAL ACCESS TOKEN
        // =================================================

        await ValidateAccessTokenAsync(
            context,
            userIdText,
            cancellationToken);
    }

    // =====================================================
    // VALIDATE APPEAL TOKEN
    // =====================================================

    private async Task ValidateAppealTokenAsync(
        TokenValidatedContext context,
        string? userIdText,
        CancellationToken ct)
    {
        // =================================================
        // VERIFY TOKEN SUBJECT AND ENDPOINT
        // =================================================

        if (!Guid.TryParse(
                userIdText,
                out var appealUserId) ||
            !context.Request.Path.StartsWithSegments(
                "/api/appeals"))
        {
            context.Fail(
                "Appeal token is restricted");

            return;
        }

        // =================================================
        // LOAD USER
        // =================================================

        var appealUser =
            await db.Users.FindAsync(
                new object?[] { appealUserId },
                ct);

        // =================================================
        // CHECK ACTIVE SUSPENSION
        // =================================================

        var now = clock.GetUtcNow().UtcDateTime;

        var appealSuspended =
            await db.AccountSuspensions.AnyAsync(
                suspension =>
                    suspension.UserId == appealUserId &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (
                        suspension.ExpiresAt == null ||
                        suspension.ExpiresAt > now
                    ),
                ct);

        // =================================================
        // CHECK APPEAL ELIGIBILITY
        // =================================================

        if (appealUser?.EmailVerifiedAt is null ||
            appealUser.DeactivatedAt is not null ||
            !appealSuspended)
        {
            context.Fail(
                "Appeal identity is no longer eligible");

            return;
        }

        // =================================================
        // APPEAL TOKEN ACCEPTED
        // =================================================

        logger.LogDebug(
            "Appeal token validated for user {UserId}",
            appealUserId);
    }

    // =====================================================
    // VALIDATE NORMAL ACCESS TOKEN
    // =====================================================

    private async Task ValidateAccessTokenAsync(
        TokenValidatedContext context,
        string? userIdText,
        CancellationToken ct)
    {
        // =================================================
        // EXTRACT SESSION ID
        // =================================================

        var sessionIdText =
            context.Principal?.FindFirstValue("sid");

        // =================================================
        // VALIDATE USER AND SESSION IDS
        // =================================================

        if (!Guid.TryParse(
                userIdText,
                out var userId) ||
            !Guid.TryParse(
                sessionIdText,
                out var sessionId))
        {
            context.Fail("Invalid session");

            return;
        }

        // =================================================
        // CURRENT UTC TIME
        // =================================================

        var now = clock.GetUtcNow().UtcDateTime;

        // =================================================
        // CHECK ACTIVE SESSION
        // =================================================

        var active =
            await db.UserSessions.AnyAsync(
                session =>
                    session.Id == sessionId &&
                    session.UserId == userId &&
                    session.RevokedAt == null,
                ct);

        // =================================================
        // LOAD USER
        // =================================================

        var user =
            await db.Users.FindAsync(
                new object?[] { userId },
                ct);

        // =================================================
        // CHECK ACCOUNT SUSPENSION
        // =================================================

        var suspended =
            await db.AccountSuspensions.AnyAsync(
                suspension =>
                    suspension.UserId == userId &&
                    suspension.Status == "Active" &&
                    suspension.RevokedAt == null &&
                    (
                        suspension.ExpiresAt == null ||
                        suspension.ExpiresAt > now
                    ),
                ct);

        // =================================================
        // CHECK ACCOUNT AND SESSION
        // =================================================

        if (!active ||
            user?.EmailVerifiedAt is null ||
            user.DeactivatedAt is not null ||
            suspended)
        {
            context.Fail(
                "Session is no longer active");

            return;
        }

        // =================================================
        // ACCESS TOKEN ACCEPTED
        // =================================================

        logger.LogDebug(
            "JWT session validated for user {UserId}",
            userId);
    }
}

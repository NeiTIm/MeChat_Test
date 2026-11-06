
using System.Security.Claims;
using System.Threading.RateLimiting;

using Microsoft.Extensions.DependencyInjection;

namespace MeChat.Api.Configurations;

/// <summary>
/// Configures global and endpoint-specific rate limiting
/// for the MeChat API.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddMeChatRateLimiting(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            // =================================================
            // REJECTION STATUS
            // =================================================

            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            // =================================================
            // GLOBAL LIMITER
            // =================================================

            // 600 requests per minute per remote IP.

            options.GlobalLimiter =
                PartitionedRateLimiter.Create<HttpContext, string>(
                    context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            context.Connection.RemoteIpAddress?
                                .ToString() ?? "unknown",
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 600,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0
                            }));

            // =================================================
            // MESSAGES POLICY
            // =================================================

            // 120 requests per minute per user,
            // falling back to IP when no identity is present.

            options.AddPolicy(
                "messages",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.User.FindFirstValue(
                            ClaimTypes.NameIdentifier)
                        ?? context.Connection.RemoteIpAddress?
                            .ToString()
                        ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

            // =================================================
            // AUTH POLICY
            // =================================================

            // 30 requests per minute per IP.

            options.AddPolicy(
                "auth",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?
                            .ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

            // =================================================
            // UPLOAD POLICY
            // =================================================

            // 20 requests per minute per IP.

            options.AddPolicy(
                "upload",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?
                            .ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
        });

        return services;
    }
}

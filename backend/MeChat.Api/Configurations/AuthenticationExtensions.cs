
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MeChat.Api.Configurations;

/// <summary>
/// Registers JWT Bearer authentication and authorization
/// for the MeChat API.
/// </summary>
public static class AuthenticationExtensions
{
    // =====================================================
    // REGISTER AUTHENTICATION
    // =====================================================

    public static IServiceCollection AddMeChatAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // =================================================
        // JWT SIGNING KEY
        // =================================================

        var signingKey = configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(signingKey) ||
            Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be configured in local secret storage.");
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        // =================================================
        // JWT VALIDATION EVENTS
        // =================================================

        // JwtSessionValidationEvents is resolved from DI.
        // The event handler can safely use scoped
        // dependencies, including AppDbContext.

        services.AddScoped<JwtSessionValidationEvents>();

        // =================================================
        // JWT BEARER AUTHENTICATION
        // =================================================

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // =========================================
                // TOKEN VALIDATION
                // =========================================

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "MeChat.Api",

                        ValidateAudience = true,
                        ValidAudience = "MeChat.Frontend",

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key
                    };

                // =========================================
                // CUSTOM JWT EVENTS
                // =========================================

                options.EventsType =
                    typeof(JwtSessionValidationEvents);
            });

        // =================================================
        // AUTHORIZATION
        // =================================================

        services.AddAuthorization();

        return services;
    }
}

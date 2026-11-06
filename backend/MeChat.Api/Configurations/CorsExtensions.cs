
using Microsoft.Extensions.DependencyInjection;

namespace MeChat.Api.Configurations;

/// <summary>
/// Configures Cross-Origin Resource Sharing
/// for the MeChat frontend.
/// </summary>
public static class CorsExtensions
{
    public const string LocalFrontendPolicy = "LocalFrontend";

    // =====================================================
    // REGISTER CORS
    // =====================================================

    public static IServiceCollection AddMeChatCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCors(options =>
        {
            options.AddPolicy(
                LocalFrontendPolicy,
                policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:5173",
                            "http://127.0.0.1:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        return services;
    }
}

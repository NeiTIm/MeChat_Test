
using Microsoft.Extensions.DependencyInjection;

using MeChat.Api.Common.Swagger;

namespace MeChat.Api.Configurations;

/// <summary>
/// Registers and configures OpenAPI documentation
/// and the Swagger UI for MeChat.
/// </summary>
public static class OpenApiExtensions
{
    // =====================================================
    // REGISTER OPENAPI
    // =====================================================

    public static IServiceCollection AddMeChatOpenApi(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(options =>
        {
            // Add JWT Bearer authentication
            // metadata to the OpenAPI document.

            options.AddDocumentTransformer<
                BearerSecurityTransformer>();
        });

        return services;
    }

    // =====================================================
    // CONFIGURE SWAGGER UI
    // =====================================================

    public static WebApplication UseMeChatSwagger(
        this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // =================================================
        // DEVELOPMENT ONLY
        // =================================================

        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        // =================================================
        // OPENAPI JSON ENDPOINT
        // =================================================

        app.MapOpenApi();

        // =================================================
        // SWAGGER UI
        // =================================================

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/openapi/v1.json",
                "MeChat API v1");

            options.RoutePrefix = "swagger";

            options.DocumentTitle =
                "MeChat - API Documentation";

            options.DisplayRequestDuration();

            options.EnableDeepLinking();
        });

        return app;
    }
}

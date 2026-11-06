
using MeChat.Api.Common;
using MeChat.Api.Configurations;
using MeChat.Api.Infrastructure;

using MeChat.Api.Features.Auth;
using MeChat.Api.Features.Users;
using MeChat.Api.Features.Friends;
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Messages;
using MeChat.Api.Features.Presence;
using MeChat.Api.Features.Files;
using MeChat.Api.Features.Notifications;
using MeChat.Api.Features.Moderation;

using MeChat.Api.Realtime;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// LOGGING
// =====================================================

builder.Logging.AddFilter(
    "Microsoft.EntityFrameworkCore.Database.Command",
    LogLevel.Warning);

// =====================================================
// CORE SERVICES
// =====================================================

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// =====================================================
// INFRASTRUCTURE
// =====================================================

builder.Services.AddMeChatInfrastructure(
    builder.Configuration);

// =====================================================
// AUTHENTICATION & AUTHORIZATION
// =====================================================

builder.Services.AddMeChatAuthentication(
    builder.Configuration);

// =====================================================
// APPLICATION CONFIGURATION
// =====================================================

builder.Services.AddMeChatOpenApi();

builder.Services.AddMeChatRateLimiting();

builder.Services.AddMeChatCors(
    builder.Configuration);

// =====================================================
// FEATURES
// =====================================================

builder.Services.AddAuthFeature();

builder.Services.AddUsersFeature();

builder.Services.AddFriendsFeature();

builder.Services.AddConversationsFeature();

builder.Services.AddGroupsFeature();

builder.Services.AddMessagesFeature();

builder.Services.AddPresenceFeature();

builder.Services.AddFilesFeature(
    builder.Configuration);

builder.Services.AddNotificationsFeature();

builder.Services.AddModerationFeature(
    builder.Configuration);

// =====================================================
// REALTIME
// =====================================================

builder.Services.AddMeChatRealtime();

// =====================================================
// BUILD APPLICATION
// =====================================================

var app = builder.Build();

// =====================================================
// EXCEPTION HANDLING
// =====================================================

app.UseExceptionHandler();

// =====================================================
// OPENAPI / SWAGGER (DEVELOPMENT ONLY)
// =====================================================

app.UseMeChatSwagger();

// =====================================================
// MIDDLEWARE PIPELINE
// =====================================================

app.UseCors("LocalFrontend");

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

// =====================================================
// CONTROLLER ENDPOINTS
// =====================================================

app.MapControllers();

// =====================================================
// SIGNALR HUB
// =====================================================

app.MapHub<ChatHub>(
    "/hubs/chat",
    options =>
        options.CloseOnAuthenticationExpiration = true);

// =====================================================
// HEALTH CHECK
// =====================================================

app.MapGet(
        "/api/health",
        () => Results.Ok(
            new HealthResponse(
                "ok",
                "MeChat.Api")))
    .WithName("GetHealth");

// =====================================================
// START APPLICATION
// =====================================================

app.Run();

public partial class Program;

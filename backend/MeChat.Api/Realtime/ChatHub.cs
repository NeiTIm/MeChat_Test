using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Presence;
using MeChat.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MeChat.Api.Realtime;

[Authorize]
public sealed class ChatHub(PrivateChatService chats, GroupService groups, PresenceService presence, AppDbContext db, SessionConnections connections) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = Guid.Parse(Context.User!.FindFirstValue("sid")!);
        connections.Register(Context.ConnectionId, userId, sessionId, Context.Abort);
        // Register before rechecking so revocation cannot race a newly connected device.
        if (!await db.UserSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId && s.RevokedAt == null,
            Context.ConnectionAborted)
            || !await db.Users.AnyAsync(u => u.Id == userId && u.EmailVerifiedAt != null && u.DeactivatedAt == null, Context.ConnectionAborted)
            || await db.AccountSuspensions.AnyAsync(s => s.UserId == userId && s.Status == "Active" && s.RevokedAt == null
                && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow), Context.ConnectionAborted))
        {
            connections.Remove(Context.ConnectionId);
            Context.Abort();
            return;
        }
        await presence.ConnectedAsync(userId, Context.ConnectionId, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        connections.Remove(Context.ConnectionId);
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await presence.DisconnectedAsync(userId, Context.ConnectionId, CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartTyping(Guid conversationId)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = Guid.Parse(Context.User!.FindFirstValue("sid")!);
        if (!await presence.TypingAsync(userId, sessionId, conversationId, true, Context.ConnectionAborted))
            throw new HubException("Typing is not available.");
    }

    public async Task StopTyping(Guid conversationId)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = Guid.Parse(Context.User!.FindFirstValue("sid")!);
        if (!await presence.TypingAsync(userId, sessionId, conversationId, false, Context.ConnectionAborted))
            throw new HubException("Typing is not available.");
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = Guid.Parse(Context.User!.FindFirstValue("sid")!);
        if (!await db.UserSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId && s.RevokedAt == null, Context.ConnectionAborted))
            throw new HubException("Session is no longer active.");
        if (!await chats.CanReadAsync(userId, conversationId, Context.ConnectionAborted)
            && !await chats.CanReadRequestAsync(userId, conversationId, Context.ConnectionAborted)
            && !await groups.CanSendAsync(userId, conversationId, Context.ConnectionAborted))
            throw new HubException("Conversation is not available.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}", Context.ConnectionAborted);
    }

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}", Context.ConnectionAborted);
}

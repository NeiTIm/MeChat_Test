using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Realtime;

public sealed class StateEvents(AppDbContext db, IHubContext<ChatHub> hub, ILogger<StateEvents> logger)
{
    public async Task SendAsync(IEnumerable<Guid> recipients, string name, object payload, CancellationToken ct)
    {
        foreach (var user in recipients.Distinct())
        {
            try { await hub.Clients.User(user.ToString()).SendAsync(name, payload, ct); }
            catch (Exception error) { logger.LogWarning(error, "State event {EventName} delivery failed", name); }
        }
    }

    public async Task GroupChangedAsync(Guid id, CancellationToken ct, Guid? affectedUser = null)
    {
        try
        {
        var recipients = await db.ConversationMembers.Where(m => m.ConversationId == id && m.LeftAt == null).Select(m => m.UserId).ToListAsync(ct);
        if (affectedUser is { } user) recipients.Add(user);
        // IDs only, never message/profile/evidence content; removed users must discard and reauthorize their cache.
        await SendAsync(recipients, "GroupChanged", new { eventId = Guid.NewGuid(), conversationId = id }, ct);
        }
        catch (Exception error) { logger.LogWarning(error, "Group state recovery event delivery failed"); }
    }
}

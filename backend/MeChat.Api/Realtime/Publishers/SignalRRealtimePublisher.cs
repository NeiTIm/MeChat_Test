using Microsoft.AspNetCore.SignalR;
using MeChat.Api.Realtime.Interfaces;

namespace MeChat.Api.Realtime.Publishers;

public sealed class SignalRRealtimePublisher(IHubContext<ChatHub> hub) : IRealtimePublisher
{
    public Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken ct) =>
        hub.Clients.User(userId).SendAsync(eventName, payload, ct);
}

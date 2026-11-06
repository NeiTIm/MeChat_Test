namespace MeChat.Api.Realtime.Interfaces;

public interface IRealtimePublisher
{
    Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken ct);
}

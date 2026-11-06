using System.Collections.Concurrent;

namespace MeChat.Api.Realtime;

// Single API instance registry. A distributed deployment needs a shared revocation bus.
public sealed class SessionConnections
{
    private sealed record Connection(Guid UserId, Guid SessionId, Action Abort);
    private readonly ConcurrentDictionary<string, Connection> _connections = new();

    public void Register(string id, Guid user, Guid session, Action abort) =>
        _connections[id] = new Connection(user, session, abort);

    public void Remove(string id) => _connections.TryRemove(id, out _);

    public void Revoke(Guid user, Guid? session = null, Guid? exceptSession = null)
    {
        foreach (var pair in _connections)
        {
            var connection = pair.Value;
            if (connection.UserId != user || session.HasValue && connection.SessionId != session
                || exceptSession.HasValue && connection.SessionId == exceptSession) continue;
            if (_connections.TryRemove(pair.Key, out _)) connection.Abort();
        }
    }
}

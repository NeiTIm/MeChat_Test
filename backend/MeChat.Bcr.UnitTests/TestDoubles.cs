using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using MeChat.Api.Realtime;

namespace MeChat.Bcr.UnitTests;

public class StubProxy : DispatchProxy
{
    public Func<MethodInfo, object?[], object?> Handler = (_, _) => throw new NotSupportedException();
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args ?? []);
    public static T Create<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        var proxy = Create<T, StubProxy>();
        ((StubProxy)(object)proxy).Handler = handler;
        return proxy;
    }
}

public sealed class RecordingHub : IHubContext<ChatHub>
{
    public List<(string User, string Event, object Payload)> Sent { get; } = [];
    public List<(string User, string Event)> Attempts { get; } = [];
    public Action? BeforeSend { get; set; }
    public bool Fail { get; set; }
    public IHubClients Clients => StubProxy.Create<IHubClients>((method, args) =>
        method.Name == "User" ? new Client(this, (string)args[0]!) : throw new NotSupportedException(method.Name));
    public IGroupManager Groups => throw new NotSupportedException();
    public StateEvents Events => new(null!, this, NullLogger<StateEvents>.Instance);
    private sealed class Client(RecordingHub owner, string user) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken ct = default)
        {
            owner.Attempts.Add((user, method));
            owner.BeforeSend?.Invoke();
            if (owner.Fail) throw new InvalidOperationException("Test-only transport failure");
            owner.Sent.Add((user, method, args[0]!));
            return Task.CompletedTask;
        }
    }
}

public sealed class FixedClock : TimeProvider
{
    public static readonly DateTime Instant = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
    public override DateTimeOffset GetUtcNow() => new(Instant);
}

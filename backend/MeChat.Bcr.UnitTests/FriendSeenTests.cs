using System.Reflection;
using System.Text.Json;
using MeChat.Api.Configurations;
using MeChat.Api.Features.Friends;
using MeChat.Api.Features.Friends.Models;
using MeChat.Api.Features.Friends.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace MeChat.Bcr.UnitTests;

public sealed class FriendSeenTests
{
    private readonly Guid actor = Guid.NewGuid(), other = Guid.NewGuid();
    private readonly List<FriendRequest> rows = [];
    private readonly RecordingHub hub = new();
    private bool committed, failCommit, retryFirst;
    private int updateCalls;
    private Guid[] sentIds = [];

    private FriendService Service(bool enabled = true)
    {
        hub.BeforeSend = () => Assert.True(committed, "Events must follow commit.");
        var repo = StubProxy.Create<IFriendRepository>((method, args) =>
        {
            switch (method.Name)
            {
                case "ExecuteAtomicAsync": return GetType().GetMethod(nameof(Atomic), BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(method.GetGenericArguments()).Invoke(this, [args[0]]);
                case "CountEligibleUsersAsync": return Task.FromResult(2);
                case "UserExistsAsync": return Task.FromResult(true);
                case "SaveChangesAsync": return Task.FromResult(1);
                case "AddFriendRequest": rows.Add((FriendRequest)args[0]!); return null;
                case "AddFriendship": case "AddUserBlock": return null;
                case "GetPendingPairRequestAsync": return Task.FromResult(rows.FirstOrDefault(r => r.Status == "Pending"));
                case "GetPendingReceivedRequestAsync": return Task.FromResult(rows.FirstOrDefault(r => r.Id == (Guid)args[0]! && r.ReceiverUserId == (Guid)args[1]! && r.Status == "Pending"));
                case "GetPendingSentRequestAsync": return Task.FromResult(rows.FirstOrDefault(r => r.Id == (Guid)args[0]! && r.SenderUserId == (Guid)args[1]! && r.Status == "Pending"));
                case "GetPendingPairRequestsAsync": return Task.FromResult(rows.Where(r => r.Status == "Pending").ToList());
                case "GetUnseenCountAsync": return Task.FromResult(rows.Count(r => r.ReceiverUserId == (Guid)args[0]! && r.Status == "Pending" && r.SeenAt == null));
                case "MarkSeenAsync":
                    updateCalls++; sentIds = (Guid[])args[1]!;
                    var changes = rows.Where(r => r.ReceiverUserId == (Guid)args[0]! && r.Status == "Pending" && r.SeenAt == null && sentIds.Contains(r.Id)).ToArray();
                    foreach (var r in changes) r.SeenAt = (DateTime)args[2]!;
                    return Task.FromResult(changes.Length);
            }
            // Defaults for unrelated eligibility/grant reads only, never database work.
            var type = method.ReturnType;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var valueType = type.GetGenericArguments()[0];
                var value = valueType.IsValueType || valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>)
                    ? Activator.CreateInstance(valueType) : null;
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(valueType).Invoke(null, [value]);
            }
            throw new NotSupportedException(method.Name);
        });
        return new(repo, new FixedClock(), hub.Events, Options.Create(new BcrFeatureOptions { FriendRequestSeenEnabled = enabled }));
    }

    private async Task<T> Atomic<T>(Func<Task<T>> operation)
    {
        committed = false;
        if (retryFirst) { retryFirst = false; await operation(); rows.Clear(); }
        var result = await operation();
        if (failCommit) throw new InvalidOperationException("Test-only commit failure");
        committed = true; return result;
    }

    private FriendRequest Add(Guid? receiver = null, string status = "Pending", bool seen = false, Guid? sender = null)
    {
        var r = new FriendRequest { Id = Guid.NewGuid(), SenderUserId = sender ?? other, ReceiverUserId = receiver ?? actor,
            PairUserId1 = actor, PairUserId2 = other, CreatedAt = FixedClock.Instant.AddDays(-1), Status = status,
            SeenAt = seen ? FixedClock.Instant.AddHours(-1) : null };
        rows.Add(r); return r;
    }

    [Fact]
    public async Task Mixed_batch_only_marks_received_pending_unseen_and_replay_keeps_timestamp()
    {
        var own = Add(); var alien = Add(Guid.NewGuid()); var outgoing = Add(other, sender: actor); var seen = Add(seen: true); var done = Add(status: "Accepted");
        var firstSeen = seen.SeenAt; var service = Service();
        Guid[] ids = [own.Id, own.Id, alien.Id, outgoing.Id, seen.Id, done.Id, Guid.NewGuid()];
        Assert.Equal(1, await service.MarkSeenAsync(actor, ids, default));
        Assert.Equal(ids.Length - 1, sentIds.Length); Assert.Equal(FixedClock.Instant, own.SeenAt);
        Assert.Null(alien.SeenAt); Assert.Null(outgoing.SeenAt); Assert.Null(done.SeenAt); Assert.Equal(firstSeen, seen.SeenAt);
        Assert.Equal("Pending", own.Status); Assert.Null(own.RespondedAt);
        Assert.Equal(0, await service.MarkSeenAsync(actor, ids, default));
        Assert.Single(hub.Sent); Assert.Equal(actor.ToString(), hub.Sent[0].User);
        Assert.Equal("FriendRequestsUnreadChanged", hub.Sent[0].Event);
        var json = JsonSerializer.Serialize(hub.Sent[0].Payload);
        Assert.DoesNotContain(own.Id.ToString(), json); Assert.DoesNotContain("count", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Entire_invalid_batch_is_rejected_before_repository_update()
    {
        var own = Add(); var service = Service();
        await Assert.ThrowsAsync<ArgumentException>(() => service.MarkSeenAsync(actor, [own.Id, Guid.Empty], default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.MarkSeenAsync(actor, Enumerable.Repeat(own.Id, 101).ToArray(), default));
        Assert.Equal(0, updateCalls); Assert.Null(own.SeenAt); Assert.Empty(hub.Sent);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Batch_limit_is_raw_before_deduplication(int count, bool valid)
    {
        Assert.Equal(valid, FriendRequestSeenInput.TryNormalize(Enumerable.Repeat(Guid.NewGuid(), count).ToArray(), out var ids));
        if (valid) Assert.Single(ids);
        Assert.False(FriendRequestSeenInput.TryNormalize(null, out _));
        Assert.False(FriendRequestSeenInput.TryNormalize([Guid.Empty], out _));
    }

    [Fact]
    public async Task Get_count_never_changes_pending_or_seen_and_excludes_outgoing()
    {
        var own = Add(); Add(seen: true); Add(status: "Rejected"); Add(other, sender: actor);
        var service = Service();
        Assert.Equal(1, await service.UnseenCountAsync(actor, default)); Assert.Equal(1, await service.UnseenCountAsync(actor, default));
        Assert.Null(own.SeenAt); Assert.Equal(0, updateCalls); Assert.Empty(hub.Sent);
    }

    [Fact]
    public async Task Create_and_reverse_autoaccept_notify_only_affected_receiver()
    {
        var service = Service();
        Assert.Equal(FriendActionResult.Created, await service.SendAsync(other, actor, default));
        Assert.Null(rows.Single().SeenAt);
        Assert.Single(hub.Sent, e => e.Event == "FriendRequestsUnreadChanged" && e.User == actor.ToString());
        hub.Sent.Clear();
        Assert.Equal(FriendActionResult.Accepted, await service.SendAsync(actor, other, default));
        Assert.Equal("Accepted", rows.Single().Status);
        Assert.Single(hub.Sent, e => e.Event == "FriendRequestsUnreadChanged" && e.User == actor.ToString());
        Assert.Equal(2, hub.Sent.Count(e => e.Event == "FriendshipChanged"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Respond_preserves_existing_result_and_invalidates_receiver(bool accept)
    {
        var request = Add(); var service = Service();
        Assert.Equal(accept ? FriendActionResult.Accepted : FriendActionResult.Created, await service.RespondAsync(actor, request.Id, accept, default));
        Assert.Equal(accept ? "Accepted" : "Rejected", request.Status); Assert.Null(request.SeenAt);
        Assert.Single(hub.Sent, e => e.Event == "FriendRequestsUnreadChanged" && e.User == actor.ToString());
    }

    [Fact]
    public async Task Cancel_notifies_receiver_and_block_preserves_permissions_event()
    {
        var request = Add(); var service = Service();
        Assert.True(await service.CancelAsync(other, request.Id, default));
        Assert.Equal("Cancelled", request.Status);
        Assert.Single(hub.Sent, e => e.Event == "FriendRequestsUnreadChanged" && e.User == actor.ToString());
        hub.Sent.Clear(); var pending = Add();
        Assert.True(await service.BlockAsync(other, actor, default)); Assert.Equal("Blocked", pending.Status);
        Assert.Single(hub.Sent, e => e.Event == "FriendRequestsUnreadChanged" && e.User == actor.ToString());
        Assert.Equal(2, hub.Sent.Count(e => e.Event == "PermissionsChanged"));
    }

    [Fact]
    public async Task Failed_commit_never_emits_and_retry_clears_old_callbacks()
    {
        var request = Add(); failCommit = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().MarkSeenAsync(actor, [request.Id], default));
        Assert.Empty(hub.Sent); Assert.Empty(hub.Attempts);
        failCommit = false; rows.Clear(); retryFirst = true;
        Assert.Equal(FriendActionResult.Created, await Service().SendAsync(other, actor, default));
        Assert.Single(hub.Sent, e => e.Event == "FriendRequestsUnreadChanged");
        Assert.Single(rows);
    }

    [Fact]
    public async Task Flag_off_preserves_new_pending_null_without_new_events()
    {
        Assert.Equal(FriendActionResult.Created, await Service(false).SendAsync(other, actor, default));
        Assert.Null(rows.Single().SeenAt); Assert.DoesNotContain(hub.Sent, e => e.Event == "FriendRequestsUnreadChanged");
        Assert.Equal(2, hub.Sent.Count(e => e.Event == "FriendshipChanged"));
    }
}

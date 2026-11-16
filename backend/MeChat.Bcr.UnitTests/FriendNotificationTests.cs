using System.Text.Json;
using MeChat.Api.Features.Friends;
using MeChat.Api.Features.Friends.Models;
using MeChat.Api.Features.Friends.Repositories.Interfaces;
using MeChat.Api.Features.Notifications;
using MeChat.Api.Features.Notifications.Models;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;

namespace MeChat.Bcr.UnitTests;

// Service/repository doubles only. No DbContext, connection string or SQL writes.
public sealed class FriendNotificationTests
{
    private readonly Guid sender = Guid.NewGuid(), receiver = Guid.NewGuid();
    private readonly List<FriendRequest> requests = [];
    private readonly List<Notification> notifications = [];
    private readonly RecordingHub hub = new();
    private bool committed, failCommit, retry, blocked, friends;
    private int eligible = 2, saves;

    private FriendService Service()
    {
        hub.BeforeSend = () => Assert.True(committed);
        var repository = StubProxy.Create<IFriendRepository>((method, args) => method.Name switch
        {
            "ExecuteAtomicAsync" => Atomic((Func<Task<FriendActionResult>>)args[0]!),
            "CountEligibleUsersAsync" => Task.FromResult(eligible),
            "HasActiveSuspensionAsync" => Task.FromResult(false),
            "HasActiveBlockAsync" => Task.FromResult(blocked),
            "HasActiveFriendshipAsync" => Task.FromResult(friends),
            "GetPendingPairRequestAsync" => Task.FromResult(requests.SingleOrDefault(r => r.Status == "Pending")),
            "GetPrivateConversationPairAsync" => Task.FromResult<MeChat.Api.Features.Conversations.Models.PrivateConversationPair?>(null),
            "AddFriendRequest" => Add(requests, (FriendRequest)args[0]!),
            "AddNotification" => Add(notifications, (Notification)args[0]!),
            "AddFriendship" => null,
            "SaveChangesAsync" => Save(),
            _ => throw new NotSupportedException(method.Name)
        });
        return new(repository, new FixedClock(), hub.Events);
    }

    private static object? Add<T>(List<T> rows, T row) { rows.Add(row); return null; }
    private Task<int> Save()
    {
        Assert.Single(requests);
        Assert.Single(notifications);
        Assert.Equal(requests[0].Id, notifications[0].ReferenceId);
        saves++;
        return Task.FromResult(2);
    }
    private async Task<FriendActionResult> Atomic(Func<Task<FriendActionResult>> operation)
    {
        committed = false;
        var requestCount = requests.Count;
        var notificationCount = notifications.Count;
        if (retry)
        {
            retry = false;
            await operation();
            requests.RemoveRange(requestCount, requests.Count - requestCount);
            notifications.RemoveRange(notificationCount, notifications.Count - notificationCount);
        }
        var result = await operation();
        if (failCommit)
        {
            requests.RemoveRange(requestCount, requests.Count - requestCount);
            notifications.RemoveRange(notificationCount, notifications.Count - notificationCount);
            throw new InvalidOperationException("Test-only commit failure");
        }
        committed = true;
        return result;
    }

    [Fact]
    public async Task New_request_and_notification_are_saved_together_and_published_to_receiver_after_commit()
    {
        Assert.Equal(FriendActionResult.Created, await Service().SendAsync(sender, receiver, default));
        var notification = Assert.Single(notifications);
        var request = Assert.Single(requests);
        Assert.Equal(receiver, notification.RecipientUserId);
        Assert.Equal(sender, notification.ActorUserId);
        Assert.Equal("FriendRequest", notification.Type);
        Assert.Equal(request.Id, notification.ReferenceId);
        Assert.Equal(request.CreatedAt, notification.CreatedAt);
        Assert.Null(request.SeenAt); Assert.Equal("Pending", request.Status);
        Assert.Null(notification.ReadAt); Assert.Null(notification.DeletedAt);
        var sent = Assert.Single(hub.Sent, e => e.Event == "NotificationCreated");
        Assert.Equal(receiver.ToString(), sent.User);
        var payload = JsonSerializer.Serialize(sent.Payload);
        Assert.Contains(notification.Id.ToString(), payload);
        Assert.Contains(request.Id.ToString(), payload);
        Assert.DoesNotContain("SeenAt", payload);
    }

    [Fact]
    public async Task Duplicate_and_reverse_autoaccept_do_not_create_another_notification()
    {
        var service = Service();
        Assert.Equal(FriendActionResult.Created, await service.SendAsync(sender, receiver, default));
        Assert.Equal(FriendActionResult.Conflict, await service.SendAsync(sender, receiver, default));
        Assert.Equal(1, saves);
        Assert.Equal(FriendActionResult.Accepted, await service.SendAsync(receiver, sender, default));
        Assert.Single(notifications);
        Assert.Single(hub.Sent, e => e.Event == "NotificationCreated");
        Assert.Equal("Accepted", requests[0].Status); Assert.Null(requests[0].SeenAt);
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("ineligible")]
    [InlineData("friends")]
    [InlineData("self")]
    [InlineData("empty")]
    public async Task Rejected_sends_do_not_persist_or_publish_notifications(string reason)
    {
        blocked = reason == "blocked"; eligible = reason == "ineligible" ? 0 : 2; friends = reason == "friends";
        var target = reason == "self" ? sender : reason == "empty" ? Guid.Empty : receiver;
        var result = await Service().SendAsync(sender, target, default);
        Assert.NotEqual(FriendActionResult.Created, result);
        Assert.Empty(requests); Assert.Empty(notifications); Assert.Empty(hub.Sent); Assert.Equal(0, saves);
    }

    [Fact]
    public async Task Commit_failure_rolls_back_both_records_and_publishes_nothing()
    {
        failCommit = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().SendAsync(sender, receiver, default));
        Assert.Empty(requests); Assert.Empty(notifications); Assert.Empty(hub.Sent);
    }

    [Fact]
    public async Task Transaction_retry_discards_previous_attempt_events_and_notifications()
    {
        retry = true;
        Assert.Equal(FriendActionResult.Created, await Service().SendAsync(sender, receiver, default));
        Assert.Single(requests); var row = Assert.Single(notifications);
        var sent = Assert.Single(hub.Sent, e => e.Event == "NotificationCreated");
        Assert.Contains(row.Id.ToString(), JsonSerializer.Serialize(sent.Payload));
    }

    [Fact]
    public async Task Realtime_failure_does_not_change_committed_send_result()
    {
        hub.Fail = true;
        Assert.Equal(FriendActionResult.Created, await Service().SendAsync(sender, receiver, default));
        Assert.True(committed); Assert.Single(requests); Assert.Single(notifications);
        Assert.Contains(hub.Attempts, e => e.Event == "NotificationCreated" && e.User == receiver.ToString());
    }

    [Fact]
    public async Task List_and_trash_resolve_only_recipient_owned_request_text()
    {
        var row = new Notification { Id = Guid.NewGuid(), Type = "FriendRequest", ReferenceId = Guid.NewGuid(),
            RecipientUserId = receiver, DeletedAt = FixedClock.Instant };
        var repository = StubProxy.Create<INotificationRepository>((method, args) => method.Name switch
        {
            "GetUnreadMessageCandidatesAsync" => Task.FromResult(new List<MessageNotificationCandidate>()),
            "GetNotificationPageAsync" or "GetTrashPageAsync" => Task.FromResult(
                (Guid)args[0]! == receiver ? new List<Notification> { row } : []),
            "GetFriendRequestSenderNameAsync" => (Guid)args[0]! == row.ReferenceId && (Guid)args[1]! == receiver
                ? Task.FromResult<string?>("Người gửi") : throw new InvalidOperationException("Wrong ownership scope"),
            _ => throw new NotSupportedException(method.Name)
        });
        var service = new NotificationService(repository, null!, null!, null!, new FixedClock(), hub.Events);
        Assert.Equal("Người gửi đã gửi cho bạn một lời mời kết bạn.",
            (await service.ListAsync(receiver, null, 20, default)).Items.Single().Text);
        Assert.Equal("Người gửi đã gửi cho bạn một lời mời kết bạn.",
            (await service.TrashAsync(receiver, null, null, 20, default)).Items.Single().Text);
        Assert.Empty((await service.ListAsync(sender, null, 20, default)).Items);
    }
}

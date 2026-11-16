using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Groups.Repositories.Interfaces;
using MeChat.Api.Features.Messages;
using MeChat.Api.Features.Messages.Models;
using MeChat.Api.Features.Messages.Repositories.Interfaces;
using MeChat.Api.Features.Notifications;
using MeChat.Api.Features.Notifications.Models;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MeChat.Bcr.UnitTests;

// LINQ/service doubles and SQL translation only. Never opens a database connection.
public sealed class MessageNotificationGroupingTests
{
    private readonly Guid actor = Guid.NewGuid();
    private readonly Guid room = Guid.NewGuid();
    private readonly List<Notification> rows = [];
    private readonly List<Message> messages = [];
    private readonly HashSet<Guid> hidden = [];
    private readonly RecordingHub hub = new();

    private Notification Add(long sequence, string type = "GroupMessage", Guid? conversation = null, Guid? owner = null)
    {
        var message = new Message { Id = Guid.NewGuid(), ConversationId = conversation ?? room,
            Sequence = sequence, SenderUserId = Guid.NewGuid() };
        messages.Add(message);
        var row = new Notification { Id = Guid.NewGuid(), RecipientUserId = owner ?? actor, Type = type,
            ActorUserId = message.SenderUserId, ReferenceId = message.Id, CreatedAt = FixedClock.Instant.AddSeconds(sequence) };
        rows.Add(row);
        return row;
    }

    private List<MessageNotificationCandidate> Candidates(Guid owner) =>
        (from row in rows join message in messages on row.ReferenceId equals (Guid?)message.Id
         where row.RecipientUserId == owner && row.ReadAt == null && row.DeletedAt == null && MessageNotificationGrouping.IsGrouped(row.Type)
         select new MessageNotificationCandidate(row, message.ConversationId, message.Sequence)).ToList();

    private IQueryable<Notification> ActionRows(Guid id, Guid owner) =>
        MessageNotificationGrouping.ActionRows(rows.AsQueryable(), messages.AsQueryable(), id, owner);

    private NotificationService Service()
    {
        var repository = StubProxy.Create<INotificationRepository>((method, args) =>
        {
            switch (method.Name)
            {
                case "GetUnreadMessageCandidatesAsync": return Task.FromResult(Candidates((Guid)args[0]!));
                case "GetFriendRequestSenderNameAsync": return Task.FromResult<string?>(null);
                case "GetUnreadNotificationsAsync": return Task.FromResult(rows.Where(n => n.RecipientUserId == (Guid)args[0]! && n.ReadAt == null && n.DeletedAt == null).ToList());
                case "GetNotificationPageAsync":
                    var representatives = (IReadOnlyCollection<Guid>)args[5]!;
                    var page = rows.Where(n => n.RecipientUserId == (Guid)args[0]! && n.DeletedAt == null &&
                        (n.ReadAt != null || !MessageNotificationGrouping.IsGrouped(n.Type) || representatives.Contains(n.Id)));
                    if (args[1] is DateTime before) page = page.Where(n => n.CreatedAt < before);
                    return Task.FromResult(page.OrderByDescending(n => n.CreatedAt).Take((int)args[3]!).ToList());
                case "GetNotificationAsync":
                case "GetLifecycleNotificationAsync":
                    return Task.FromResult(rows.SingleOrDefault(n => n.Id == (Guid)args[0]! && n.RecipientUserId == (Guid)args[1]! &&
                        (method.Name == "GetLifecycleNotificationAsync" || n.DeletedAt == null)));
                case "MarkReadAsync":
                    var read = ActionRows((Guid)args[0]!, (Guid)args[1]!).Where(n => n.ReadAt == null && n.DeletedAt == null).ToArray();
                    foreach (var n in read) n.ReadAt = (DateTime)args[2]!;
                    return Task.FromResult(read.Length);
                case "SetDeletedAsync":
                    var time = (DateTime?)args[2];
                    var changing = time == null ? rows.Where(n => n.Id == (Guid)args[0]! && n.RecipientUserId == (Guid)args[1]!)
                        : ActionRows((Guid)args[0]!, (Guid)args[1]!);
                    var deleted = changing.Where(n => (n.DeletedAt == null) != (time == null)).ToArray();
                    foreach (var n in deleted) n.DeletedAt = time;
                    return Task.FromResult(deleted.Length);
                default: throw new NotSupportedException(method.Name);
            }
        });
        var groups = new GroupService(StubProxy.Create<IGroupRepository>((method, _) =>
            Task.FromResult(method.Name != "IsSequenceHiddenAsync")), new FixedClock(), null!, null!, null!, null!);
        var actions = new MessageActionService(StubProxy.Create<IMessageActionRepository>((method, args) => method.Name switch
        {
            "GetUnrecalledMessageAsync" => Task.FromResult(messages.SingleOrDefault(m => m.Id == (Guid)args[0]! && m.RecalledAt == null)),
            "HasMessageUserStateAsync" => Task.FromResult(hidden.Contains((Guid)args[0]!)),
            "GetConversationTypeAsync" => Task.FromResult<string?>("Group"),
            _ => throw new NotSupportedException(method.Name)
        }), new FixedClock(), null!, groups, null!, null!, null!);
        return new NotificationService(repository, null!, groups, actions, new FixedClock(), hub.Events);
    }

    [Theory]
    [InlineData("Message")]
    [InlineData("GroupMessage")]
    public void Group_key_uses_recipient_conversation_type_not_sender(string type)
    {
        var first = Add(1, type);
        for (var i = 2; i <= 5; i++) Add(i, type);
        Add(6, type, Guid.NewGuid()); Add(7, type, owner: Guid.NewGuid());
        var groups = MessageNotificationGrouping.Candidates(Candidates(actor)).ToArray();
        Assert.Equal(2, groups.Length);
        Assert.Equal(first.Id, groups[0].First().Notification.Id);
        Assert.Equal(5, groups[0].Count());
    }

    [Fact]
    public async Task Count_and_feed_group_before_pagination_and_keep_first_visible_target()
    {
        var first = Add(1);
        for (var i = 2; i <= 5; i++) Add(i);
        var other = Add(10, conversation: Guid.NewGuid());
        Add(20, "FriendRequest"); Add(21, "FriendRequest"); Add(22, "MessageRequest");
        // No sender aggregation or pagination-local distinct count.
        var service = Service();
        Assert.Equal(5, (await service.UnreadAsync(actor, default)).Count);
        var page = await service.ListAsync(actor, null, 2, default);
        Assert.Equal(2, page.Items.Count); Assert.True(page.HasMore);
        var next = await service.ListAsync(actor, page.NextCursor, 10, default, page.NextCursorId);
        Assert.Equal(3, next.Items.Count);
        Assert.Contains(next.Items, n => n.Id == first.Id && n.ReferenceId == first.ReferenceId);
        Assert.Contains(next.Items, n => n.Id == other.Id);
        hidden.Add(first.ReferenceId!.Value);
        var visible = await service.ListAsync(actor, null, 20, default);
        Assert.DoesNotContain(visible.Items, n => n.Id == first.Id);
        Assert.Contains(visible.Items, n => n.Id == rows[1].Id);
        Assert.Equal(5, (await service.UnreadAsync(actor, default)).Count);
    }

    [Fact]
    public async Task Read_closes_only_active_owned_group_and_old_replay_leaves_new_cycle_unread()
    {
        var first = Add(1); Add(2); Add(3);
        var other = Add(4, conversation: Guid.NewGuid()); var alien = Add(5, owner: Guid.NewGuid());
        var friend = Add(6, "FriendRequest"); var trash = Add(7); trash.DeletedAt = FixedClock.Instant;
        var service = Service();
        hub.BeforeSend = () => Assert.All(rows.Take(3), n => Assert.NotNull(n.ReadAt));
        Assert.True(await service.MarkReadAsync(actor, first.Id, default));
        Assert.Null(other.ReadAt); Assert.Null(alien.ReadAt); Assert.Null(friend.ReadAt); Assert.Null(trash.ReadAt);
        var fresh = Add(8);
        Assert.True(await service.MarkReadAsync(actor, first.Id, default));
        Assert.Null(fresh.ReadAt);
        Assert.All(hub.Sent, e => Assert.Equal(actor.ToString(), e.User));
    }

    [Fact]
    public async Task Delete_group_restore_preserves_read_state_and_merges_active_unread_without_duplicates()
    {
        var first = Add(1); var second = Add(2); var read = Add(3); read.ReadAt = FixedClock.Instant;
        var service = Service();
        Assert.True(await service.SetDeletedAsync(actor, first.Id, false, default));
        Assert.NotNull(first.DeletedAt); Assert.NotNull(second.DeletedAt); Assert.Null(read.DeletedAt);
        var fresh = Add(4);
        Assert.True(await service.SetDeletedAsync(actor, first.Id, false, default));
        Assert.Null(fresh.DeletedAt);
        Assert.True(await service.SetDeletedAsync(actor, first.Id, true, default));
        Assert.Null(first.ReadAt); Assert.Null(first.DeletedAt); Assert.NotNull(second.DeletedAt);
        Assert.Equal(1, (await service.UnreadAsync(actor, default)).Count);
        var page = await service.ListAsync(actor, null, 10, default);
        Assert.Single(page.Items, n => n.ReadAt == null);
        Assert.Equal(first.ReferenceId, page.Items.Single(n => n.ReadAt == null).ReferenceId);
    }

    [Fact]
    public void Conditional_actions_isolate_owner_type_and_missing_or_closed_seed()
    {
        var seed = Add(1, "Message"); var sibling = Add(2, "Message");
        Add(3, "GroupMessage"); Add(4, "FriendRequest"); Add(5, "MessageRequest");
        var alien = Add(6, "Message", owner: Guid.NewGuid());
        Assert.Equal(new[] { seed.Id, sibling.Id }, ActionRows(seed.Id, actor).Select(n => n.Id).ToArray());
        Assert.Empty(ActionRows(alien.Id, actor)); Assert.Empty(ActionRows(Guid.NewGuid(), actor));
        seed.ReadAt = FixedClock.Instant;
        Assert.Equal(seed.Id, Assert.Single(ActionRows(seed.Id, actor)).Id);
        seed.ReadAt = null; seed.DeletedAt = FixedClock.Instant;
        Assert.Equal(seed.Id, Assert.Single(ActionRows(seed.Id, actor)).Id);
        Assert.Null(sibling.ReadAt); Assert.Null(sibling.DeletedAt);
    }

    [Fact]
    public void Sql_server_translates_conditional_group_predicate_without_connecting()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unreachable.invalid;Database=TranslationOnly;Integrated Security=true;Encrypt=true").Options);
        var id = Guid.NewGuid();
        var sql = MessageNotificationGrouping.ActionRows(db.Notifications, db.Messages, id, actor)
            .Where(n => n.DeletedAt == null && n.ReadAt == null).ToQueryString();
        Assert.Contains("EXISTS", sql); Assert.Contains("ConversationId", sql);
        Assert.Contains("RecipientUserId", sql); Assert.Contains("ReadAt] IS NULL", sql);
        Assert.Contains("DeletedAt] IS NULL", sql);
        Assert.Empty(db.ChangeTracker.Entries());
    }
}

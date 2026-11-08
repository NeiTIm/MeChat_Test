using System.Text.Json;
using MeChat.Api.Features.Notifications;
using MeChat.Api.Features.Notifications.Models;
using MeChat.Api.Features.Notifications.Repositories.Interfaces;
using MeChat.Api.Features.Messages;
using MeChat.Api.Features.Messages.Models;
using MeChat.Api.Features.Messages.Repositories.Interfaces;

namespace MeChat.Bcr.UnitTests;

// Pure service tests with interface doubles. No DbContext, SQL or DefaultConnection.
public sealed class NotificationLifecycleTests
{
    private readonly Guid actor = Guid.NewGuid();
    private readonly RecordingHub hub = new();
    private readonly List<Notification> rows = [];
    private bool deleteDuringRead;
    private bool restoreDuringRead;
    private int writes;
    private NotificationService Service() => new(StubProxy.Create<INotificationRepository>((method, args) =>
    {
        Guid Id() => (Guid)args[0]!;
        Guid Owner() => (Guid)args[1]!;
        var own = rows.Where(n => n.RecipientUserId == actor);
        switch (method.Name)
        {
            case "GetLifecycleNotificationAsync": return Task.FromResult(rows.SingleOrDefault(n => n.Id == Id() && n.RecipientUserId == Owner()));
            case "GetNotificationAsync": return Task.FromResult(rows.SingleOrDefault(n => n.Id == Id() && n.RecipientUserId == Owner() && n.DeletedAt == null));
            case "SetDeletedAsync":
                var row = rows.Single(n => n.Id == Id() && n.RecipientUserId == Owner());
                var value = (DateTime?)args[2];
                if ((row.DeletedAt == null) == (value == null)) return Task.FromResult(0);
                row.DeletedAt = value; writes++; return Task.FromResult(1);
            case "MarkReadAsync":
                var reading = rows.Single(n => n.Id == Id());
                if (deleteDuringRead) reading.DeletedAt = FixedClock.Instant;
                if (reading.DeletedAt != null || reading.ReadAt != null)
                {
                    if (restoreDuringRead) reading.DeletedAt = null;
                    return Task.FromResult(0);
                }
                reading.ReadAt = (DateTime)args[2]!; return Task.FromResult(1);
            case "DeleteReadAsync":
                var read = own.Where(n => n.ReadAt != null && n.DeletedAt == null).ToArray();
                foreach (var n in read) n.DeletedAt = (DateTime)args[1]!;
                return Task.FromResult(read.Length);
            case "GetTrashPageAsync":
                var all = own.Where(n => n.DeletedAt != null).ToList();
                var offset = args[2] is Guid cursor ? all.FindIndex(n => n.Id == cursor) + 1 : 0;
                return Task.FromResult(all.Skip(offset).Take((int)args[3]!).ToList());
            case "GetNotificationPageAsync": return Task.FromResult(own.Where(n => n.DeletedAt == null).Take((int)args[3]!).ToList());
            case "GetAdminWarningTextAsync": return Task.FromResult<string?>((Guid)args[1]! == actor ? "Synthetic warning" : null);
            default: throw new NotSupportedException(method.Name);
        }
    }), null!, null!, new MessageActionService(StubProxy.Create<IMessageActionRepository>((method, _) =>
        method.Name == "GetUnrecalledMessageAsync" ? Task.FromResult<Message?>(null) : throw new NotSupportedException(method.Name)),
        new FixedClock(), null!, null!, null!, null!, null!), new FixedClock(), hub.Events);

    private Notification Add(bool read = false, bool trash = false, Guid? owner = null)
    {
        var row = new Notification { Id = Guid.NewGuid(), RecipientUserId = owner ?? actor, Type = "AdminWarning", CreatedAt = FixedClock.Instant.AddDays(-1),
            ReadAt = read ? FixedClock.Instant.AddHours(-1) : null, DeletedAt = trash ? FixedClock.Instant.AddMinutes(-1) : null };
        rows.Add(row); return row;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_restore_preserve_read_and_created_state_and_replay(bool read)
    {
        var row = Add(read); var originalRead = row.ReadAt; var created = row.CreatedAt;
        var service = Service();
        Assert.True(await service.SetDeletedAsync(actor, row.Id, false, default));
        Assert.Equal(FixedClock.Instant, row.DeletedAt);
        Assert.True(await service.SetDeletedAsync(actor, row.Id, false, default));
        Assert.Equal(1, writes); Assert.Single(hub.Sent);
        Assert.True(await service.SetDeletedAsync(actor, row.Id, true, default));
        Assert.True(await service.SetDeletedAsync(actor, row.Id, true, default));
        Assert.Null(row.DeletedAt); Assert.Equal(originalRead, row.ReadAt); Assert.Equal(created, row.CreatedAt);
        Assert.Equal(2, writes); Assert.Equal(2, hub.Sent.Count);
        Assert.All(hub.Sent, e => Assert.Equal(actor.ToString(), e.User));
        Assert.Equal("NotificationDeleted", hub.Sent[0].Event); Assert.Equal("NotificationRestored", hub.Sent[1].Event);
        Assert.DoesNotContain("Synthetic", JsonSerializer.Serialize(hub.Sent[0].Payload));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Alien_unknown_and_empty_do_not_expose_ownership(bool restore)
    {
        var row = Add(owner: Guid.NewGuid()); var service = Service();
        Assert.False(await service.SetDeletedAsync(actor, row.Id, restore, default));
        Assert.False(await service.SetDeletedAsync(actor, Guid.NewGuid(), restore, default));
        Assert.False(await service.SetDeletedAsync(actor, Guid.Empty, restore, default));
        Assert.Empty(hub.Sent); Assert.Equal(0, writes);
    }

    [Fact]
    public async Task Bulk_only_read_active_owned_and_has_no_id_payload()
    {
        var unread = Add(); var read = Add(true); var trash = Add(true, true); var other = Add(true, owner: Guid.NewGuid()); var previous = trash.DeletedAt;
        await Service().DeleteReadAsync(actor, default);
        Assert.Null(unread.DeletedAt); Assert.Equal(FixedClock.Instant, read.DeletedAt); Assert.Equal(previous, trash.DeletedAt); Assert.Null(other.DeletedAt);
        var payload = JsonSerializer.Serialize(hub.Sent.Single().Payload);
        Assert.Contains("ReadBatch", payload); Assert.Contains("\"notificationId\":null", payload);
        await Service().DeleteReadAsync(actor, default); Assert.Single(hub.Sent);
    }

    [Fact]
    public async Task Read_racing_delete_returns_unavailable_and_does_not_mark_trash_read()
    {
        var row = Add(); deleteDuringRead = true;
        Assert.False(await Service().MarkReadAsync(actor, row.Id, default));
        Assert.Null(row.ReadAt); Assert.NotNull(row.DeletedAt); Assert.Empty(hub.Sent);
    }

    [Fact]
    public async Task Already_read_active_replay_is_successful()
    {
        var row = Add(true); var readAt = row.ReadAt;
        Assert.True(await Service().MarkReadAsync(actor, row.Id, default));
        Assert.Equal(readAt, row.ReadAt);
    }

    [Fact]
    public async Task Delete_then_restore_race_cannot_report_read_success_for_still_unread_item()
    {
        var row = Add(); deleteDuringRead = true; restoreDuringRead = true;
        Assert.False(await Service().MarkReadAsync(actor, row.Id, default));
        Assert.Null(row.ReadAt); Assert.Empty(hub.Sent);
    }

    [Fact]
    public async Task Trash_page_lookahead_cursor_does_not_consume_extra_visible_item()
    {
        var first = Add(trash: true); Add(trash: true);
        var page = await Service().TrashAsync(actor, null, null, 1, default);
        Assert.Single(page.Items); Assert.True(page.HasMore); Assert.Equal(first.Id, page.NextCursorId);
        Assert.Equal(first.CreatedAt, page.NextCursor); Assert.Equal(first.DeletedAt, page.Items[0].DeletedAt);
    }

    [Fact]
    public async Task Exact_raw_batch_has_continuation_even_without_visible_lookahead()
    {
        for (var i = 0; i < 200; i++) Add(trash: true);
        var page = await Service().TrashAsync(actor, null, null, 50, default);
        Assert.True(page.HasMore); Assert.Equal(50, page.Items.Count); Assert.Equal(rows[49].Id, page.NextCursorId);
    }

    [Fact]
    public async Task Hidden_owned_item_is_unavailable_even_for_single_replay_but_bulk_still_deletes_read()
    {
        var row = Add(true); row.Type = "Message"; row.ReferenceId = Guid.NewGuid();
        var service = Service();
        Assert.False(await service.SetDeletedAsync(actor, row.Id, false, default));
        await service.DeleteReadAsync(actor, default); Assert.NotNull(row.DeletedAt);
        Assert.False(await service.SetDeletedAsync(actor, row.Id, false, default));
        Assert.False(await service.SetDeletedAsync(actor, row.Id, true, default));
    }

    [Fact]
    public async Task Hidden_only_batch_returns_advancing_raw_cursor_without_exposing_items()
    {
        for (var i = 0; i < 200; i++) { var hidden = Add(trash: true); hidden.Type = "Message"; hidden.ReferenceId = Guid.NewGuid(); }
        var visible = Add(trash: true); var service = Service();
        var page = await service.TrashAsync(actor, null, null, 30, default);
        Assert.Empty(page.Items); Assert.True(page.HasMore); Assert.Equal(rows[199].Id, page.NextCursorId);
        var next = await service.TrashAsync(actor, page.NextCursor, page.NextCursorId, 30, default);
        Assert.Equal(visible.Id, Assert.Single(next.Items).Id); Assert.False(next.HasMore); Assert.Null(next.NextCursorId);
    }

    [Fact]
    public async Task Admin_warning_text_uses_recipient_lookup_and_active_shape_is_unchanged()
    {
        var row = Add(trash: true); row.ReferenceId = Guid.NewGuid();
        var page = await Service().TrashAsync(actor, null, null, 30, default);
        Assert.Equal("Synthetic warning", page.Items.Single().Text);
        Assert.False(page.HasMore); Assert.Null(page.NextCursor); Assert.Null(page.NextCursorId);
        Assert.DoesNotContain("DeletedAt", JsonSerializer.Serialize(new NotificationDto(row.Id, row.Type, row.ReferenceId, row.CreatedAt, row.ReadAt)));
    }

    [Fact]
    public async Task Delivery_failure_after_write_does_not_undo_mutation()
    {
        var row = Add(); hub.Fail = true;
        Assert.True(await Service().SetDeletedAsync(actor, row.Id, false, default));
        Assert.NotNull(row.DeletedAt);
    }
}

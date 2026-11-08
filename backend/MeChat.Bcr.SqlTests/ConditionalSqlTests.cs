using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Notifications.Repositories;
using MeChat.Api.Features.Friends.Repositories;
using MeChat.Api.Features.Friends;
using MeChat.Api.Configurations;
using MeChat.Bcr.UnitTests;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MeChat.Bcr.SqlTests;

// NOT RUN IN G2. Separate project prevents accidental discovery by the pure unit command.
public sealed class ConditionalSqlTests : IAsyncLifetime
{
    private readonly ApprovedSqlFixture fixture = new();
    public Task InitializeAsync() => fixture.InitializeAsync();
    // Fixtures are deliberately retained for approved operator cleanup of the disposable DB.
    // No notification hard-delete, DB reset/drop or retention purge in test teardown.
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Bulk_only_deletes_owned_active_read_and_preserves_all_other_states()
    {
        await using var db = fixture.Open();
        var unread = fixture.Notice(); var read = fixture.Notice(true); var trash = fixture.Notice(true, true); var other = fixture.Notice(true, recipient: fixture.B);
        db.Notifications.AddRange(unread, read, trash, other); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(1, await new NotificationRepository(db).DeleteReadAsync(fixture.A, ApprovedSqlFixture.Now, default));
        var rows = await db.Notifications.Where(n => n.Id == unread.Id || n.Id == read.Id || n.Id == trash.Id || n.Id == other.Id).ToDictionaryAsync(n => n.Id);
        Assert.Null(rows[unread.Id].DeletedAt); Assert.NotNull(rows[read.Id].DeletedAt);
        Assert.Equal(trash.DeletedAt, rows[trash.Id].DeletedAt); Assert.Null(rows[other.Id].DeletedAt);
    }

    [Fact]
    public async Task Active_and_trash_queries_use_disjoint_states_and_recipient()
    {
        await using var db = fixture.Open(); var active = fixture.Notice(); var trash = fixture.Notice(trash: true); var other = fixture.Notice(trash: true, recipient: fixture.B);
        db.Notifications.AddRange(active, trash, other); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); var repo = new NotificationRepository(db);
        Assert.Equal(active.Id, Assert.Single(await repo.GetNotificationPageAsync(fixture.A, null, null, 200, default)).Id);
        Assert.Equal(trash.Id, Assert.Single(await repo.GetTrashPageAsync(fixture.A, null, null, 200, default)).Id);
        Assert.Null(await repo.GetNotificationAsync(trash.Id, fixture.A, default));
        Assert.Null(await repo.GetLifecycleNotificationAsync(trash.Id, fixture.B, default));
    }

    [Fact]
    public async Task Read_after_delete_is_guarded_and_restore_keeps_original_read_state()
    {
        await using var db = fixture.Open(); var row = fixture.Notice(); db.Notifications.Add(row); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var repo = new NotificationRepository(db); await repo.SetDeletedAsync(row.Id, fixture.A, ApprovedSqlFixture.Now, default);
        Assert.Equal(0, await repo.MarkReadAsync(row.Id, fixture.A, ApprovedSqlFixture.Now, default));
        Assert.Equal(1, await repo.SetDeletedAsync(row.Id, fixture.A, null, default));
        Assert.Null((await repo.GetNotificationAsync(row.Id, fixture.A, default))!.ReadAt);
    }

    [Fact]
    public async Task Keyset_uses_SQL_uniqueidentifier_tie_order_across_more_than_200_rows()
    {
        await using var db = fixture.Open(); var rows = Enumerable.Range(0, 231).Select(_ => fixture.Notice(trash: true)).ToList();
        db.Notifications.AddRange(rows); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var expected = rows.OrderByDescending(n => new SqlGuid(n.Id)).Select(n => n.Id).ToArray();
        var ids = new List<Guid>(); DateTime? before = null; Guid? beforeId = null;
        for (var page = 0; page < 10; page++)
        {
            var batch = await new NotificationRepository(db).GetTrashPageAsync(fixture.A, before, beforeId, 30, default);
            if (batch.Count == 0) break;
            ids.AddRange(batch.Select(n => n.Id)); before = batch[^1].CreatedAt; beforeId = batch[^1].Id;
        }
        Assert.Equal(expected, ids); Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task Concurrent_delete_and_replay_change_state_once()
    {
        await using var seed = fixture.Open(); var row = fixture.Notice(); seed.Notifications.Add(row); await seed.SaveChangesAsync();
        await using var a = fixture.Open(); await using var b = fixture.Open();
        var results = await Task.WhenAll(new NotificationRepository(a).SetDeletedAsync(row.Id, fixture.A, ApprovedSqlFixture.Now, default),
            new NotificationRepository(b).SetDeletedAsync(row.Id, fixture.A, ApprovedSqlFixture.Now.AddSeconds(1), default));
        Assert.Equal(1, results.Sum());
    }

    [Fact]
    public async Task Concurrent_restore_and_delete_keep_read_created_and_ownership()
    {
        await using var seed = fixture.Open(); var row = fixture.Notice(true, true); seed.Notifications.Add(row); await seed.SaveChangesAsync();
        await using var a = fixture.Open(); await using var b = fixture.Open();
        await Task.WhenAll(new NotificationRepository(a).SetDeletedAsync(row.Id, fixture.A, null, default),
            new NotificationRepository(b).SetDeletedAsync(row.Id, fixture.A, ApprovedSqlFixture.Now, default));
        seed.ChangeTracker.Clear(); var result = await seed.Notifications.SingleAsync(n => n.Id == row.Id);
        Assert.Equal(row.ReadAt, result.ReadAt); Assert.Equal(row.CreatedAt, result.CreatedAt); Assert.Equal(fixture.A, result.RecipientUserId);
    }

    [Fact]
    public async Task Read_delete_and_new_unread_bulk_race_never_marks_trash_read()
    {
        await using var seed = fixture.Open(); var row = fixture.Notice(); seed.Notifications.Add(row); await seed.SaveChangesAsync();
        await using var a = fixture.Open(); await using var b = fixture.Open();
        await Task.WhenAll(new NotificationRepository(a).MarkReadAsync(row.Id, fixture.A, ApprovedSqlFixture.Now, default),
            new NotificationRepository(b).SetDeletedAsync(row.Id, fixture.A, ApprovedSqlFixture.Now, default));
        // Valid serial outcomes: read before delete, or delete before read (ReadAt stays null).
        var incoming = fixture.Notice(); seed.Notifications.Add(incoming); await seed.SaveChangesAsync();
        await new NotificationRepository(a).DeleteReadAsync(fixture.A, ApprovedSqlFixture.Now, default);
        seed.ChangeTracker.Clear(); Assert.Null((await seed.Notifications.SingleAsync(n => n.Id == incoming.Id)).DeletedAt);
    }

    [Fact]
    public async Task Seen_is_receiver_only_monotonic_and_outgoing_projection_stays_private()
    {
        await using var db = fixture.Open(); var row = fixture.Request(); db.FriendRequests.Add(row); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); var repo = new FriendRepository(db);
        Assert.Equal(0, await repo.MarkSeenAsync(fixture.B, [row.Id], ApprovedSqlFixture.Now, default));
        Assert.Equal(1, await repo.MarkSeenAsync(fixture.A, [row.Id, Guid.NewGuid()], ApprovedSqlFixture.Now, default));
        Assert.Equal(0, await repo.MarkSeenAsync(fixture.A, [row.Id], ApprovedSqlFixture.Now.AddMinutes(1), default));
        Assert.Equal(ApprovedSqlFixture.Now, (await repo.GetFriendRequestsAsync(fixture.A, default)).Incoming.Single().SeenAt);
        Assert.Null((await repo.GetFriendRequestsAsync(fixture.B, default)).Outgoing.Single().SeenAt);
        Assert.Equal(0, await repo.GetUnseenCountAsync(fixture.A, default));
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Rejected")]
    [InlineData("Cancelled")]
    [InlineData("Blocked")]
    public async Task Seen_racing_status_transition_does_not_lose_status_or_response(string status)
    {
        await using var seed = fixture.Open(); var row = fixture.Request(); seed.FriendRequests.Add(row); await seed.SaveChangesAsync();
        await using var a = fixture.Open(); await using var b = fixture.Open();
        await Task.WhenAll(new FriendRepository(a).MarkSeenAsync(fixture.A, [row.Id], ApprovedSqlFixture.Now, default),
            b.FriendRequests.Where(r => r.Id == row.Id && r.Status == "Pending").ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, status).SetProperty(r => r.RespondedAt, (DateTime?)ApprovedSqlFixture.Now)));
        seed.ChangeTracker.Clear(); var final = await seed.FriendRequests.SingleAsync(r => r.Id == row.Id);
        Assert.Equal(status, final.Status); Assert.Equal(ApprovedSqlFixture.Now, final.RespondedAt);
        Assert.Equal(0, await new FriendRepository(seed).GetUnseenCountAsync(fixture.A, default));
    }

    [Theory]
    [InlineData("respond", "Accepted")]
    [InlineData("block", "Blocked")]
    [InlineData("cancel", "Cancelled")]
    [InlineData("reverse", "Accepted")]
    public async Task Seen_racing_real_friend_workflow_preserves_status_and_emits_after_transaction(string action, string expected)
    {
        await using var seed = fixture.Open(); var row = fixture.Request(); seed.FriendRequests.Add(row); await seed.SaveChangesAsync();
        var transactionObservations = new System.Collections.Concurrent.ConcurrentBag<bool>();
        async Task Run(Func<FriendService, Task> operation)
        {
            for (var attempt = 0; ; attempt++)
            {
                await using var db = fixture.Open(); var hub = new RecordingHub();
                hub.BeforeSend = () => transactionObservations.Add(db.Database.CurrentTransaction is not null);
                var service = new FriendService(new FriendRepository(db), new FixedClock(), hub.Events,
                    Options.Create(new BcrFeatureOptions { FriendRequestSeenEnabled = true }));
                try { await operation(service); return; }
                catch (Exception error) when (attempt < 2 && (error is SqlException { Number: 1205 } || error.InnerException is SqlException { Number: 1205 }))
                { await Task.Delay(30); } // Test coordination only; does not add production retries.
            }
        }
        await Task.WhenAll(Run(async s => { await s.MarkSeenAsync(fixture.A, [row.Id], default); }), Run(async s =>
        {
            switch (action)
            {
                case "respond": await s.RespondAsync(fixture.A, row.Id, true, default); break;
                case "block": await s.BlockAsync(fixture.A, fixture.B, default); break;
                case "cancel": await s.CancelAsync(fixture.B, row.Id, default); break;
                default: await s.SendAsync(fixture.A, fixture.B, default); break;
            }
        }));
        seed.ChangeTracker.Clear(); var result = await seed.FriendRequests.SingleAsync(r => r.Id == row.Id);
        Assert.Equal(expected, result.Status); Assert.NotNull(result.RespondedAt);
        Assert.Equal(0, await new FriendRepository(seed).GetUnseenCountAsync(fixture.A, default));
        Assert.NotEmpty(transactionObservations); Assert.DoesNotContain(true, transactionObservations);
    }
}

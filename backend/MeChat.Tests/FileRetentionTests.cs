using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Files;

namespace MeChat.Tests;

public sealed class FileRetentionTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);
    private readonly FileRetentionOptions options = new();

    [Fact]
    public void Pending_grace_boundary_requires_no_reference_and_preserves_evidence_and_avatar()
    {
        var pending = new FileRetentionFacts("Pending", Now.AddHours(-24), false, false, 0, false, null);
        Assert.Equal("ExpiredPendingUpload", FileRetentionPolicy.Reason(pending, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(pending with { CreatedAt = Now.AddHours(-24).AddTicks(1) }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(pending with { HasEvidence = true }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(pending with { HasAvatar = true }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(pending with { MessageReferences = 1 }, Now, options));
    }

    [Fact]
    public void Recall_cleanup_preserves_live_forward_and_all_evidence_even_after_retention_cutoff()
    {
        var recalled = new FileRetentionFacts("Attached", Now.AddDays(-100), false, false, 2, false, Now.AddDays(-30));
        Assert.Equal("AllMessageReferencesRecalled", FileRetentionPolicy.Reason(recalled, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(recalled with { LastRecall = Now.AddDays(-30).AddTicks(1) }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(recalled with { HasLiveMessage = true }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(recalled with { HasEvidence = true }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(recalled with { HasAvatar = true }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(recalled with { Status = "Deleted" }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(recalled with { MessageReferences = 0 }, Now, options));
    }

    [Fact]
    public void Old_unreferenced_avatar_is_eligible_but_active_reference_or_evidence_always_protects_it()
    {
        var orphan = new FileRetentionFacts("Avatar", Now.AddDays(-30), false, false, 0, false, null);
        Assert.Equal("UnreferencedAvatar", FileRetentionPolicy.Reason(orphan, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(orphan with { CreatedAt = Now.AddDays(-30).AddTicks(1) }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(orphan with { HasAvatar = true }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(orphan with { HasEvidence = true }, Now, options));
        Assert.Null(FileRetentionPolicy.Reason(orphan with { MessageReferences = 1 }, Now, options));
    }

    [Fact]
    public async Task Default_sweep_is_non_destructive_without_opening_a_database_or_storage()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(DatabaseTarget.DefaultConnection).Options);
        var service = new FileRetentionService(db, new DenyStorage(), TimeProvider.System,
            Options.Create(options), NullLogger<FileRetentionService>.Instance);
        Assert.Equal(0, await service.SweepAsync(CancellationToken.None));
    }

    private sealed class DenyStorage : IFileStorage
    {
        public void Delete(string key) => throw new InvalidOperationException("Storage must not be touched in dry run.");
        public Stream OpenRead(string key) => throw new NotSupportedException();
        public Task SaveAsync(string key, Stream source, long maximumBytes, CancellationToken ct) => throw new NotSupportedException();
    }
}

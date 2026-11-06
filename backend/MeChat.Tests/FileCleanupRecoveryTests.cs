using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Files;

namespace MeChat.Tests;

public sealed class FileCleanupRecoveryTests
{
    [Fact]
    public async Task Storage_failure_retains_tombstone_and_retry_completes_without_SQL_or_real_files()
    {
        var file = Pending(); var storage = new FakeStorage { FailNext = true }; var persisted = 0;
        Task Persist(CancellationToken _) { persisted++; return Task.CompletedTask; }
        await Assert.ThrowsAsync<IOException>(() => FileCleanupCompletion.CompleteAsync(file, storage, Persist, default));
        Assert.Equal("CleanupPending", file.Status); Assert.True(storage.Exists); Assert.Equal(0, persisted);
        await FileCleanupCompletion.CompleteAsync(file, storage, Persist, default);
        Assert.Equal("Deleted", file.Status); Assert.False(storage.Exists); Assert.Equal(1, persisted);
    }
    [Fact]
    public async Task Crash_after_blob_removal_retries_idempotently_before_final_state_is_persisted()
    {
        var file = Pending(); var storage = new FakeStorage();
        await Assert.ThrowsAsync<IOException>(() => FileCleanupCompletion.CompleteAsync(file, storage, _ => throw new IOException("Synthetic persistence outage"), default));
        Assert.False(storage.Exists); Assert.Equal("CleanupPending", file.Status);
        await FileCleanupCompletion.CompleteAsync(file, storage, _ => Task.CompletedTask, default);
        Assert.Equal("Deleted", file.Status); Assert.Equal(2, storage.DeleteCalls);
    }
    [Fact]
    public async Task No_tombstone_or_cancelled_attempt_never_touches_storage()
    {
        var file = Pending(); file.DeletedAt = null; var storage = new FakeStorage();
        await Assert.ThrowsAsync<InvalidOperationException>(() => FileCleanupCompletion.CompleteAsync(file, storage, _ => Task.CompletedTask, default));
        file.DeletedAt = DateTime.UtcNow;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileCleanupCompletion.CompleteAsync(file, storage, _ => Task.CompletedTask, new CancellationToken(true)));
        Assert.Equal(0, storage.DeleteCalls);
    }
    private static StoredFile Pending() => new() { Id = Guid.NewGuid(), StorageKey = Guid.NewGuid().ToString("N"), Status = "CleanupPending", DeletedAt = DateTime.UtcNow };
    private sealed class FakeStorage : IFileStorage
    {
        public bool Exists { get; private set; } = true;
        public bool FailNext { get; set; }
        public int DeleteCalls { get; private set; }
        public void Delete(string key) { DeleteCalls++; if (FailNext) { FailNext = false; throw new IOException("Synthetic storage outage"); } Exists = false; }
        public Stream OpenRead(string key) => throw new NotSupportedException();
        public Task SaveAsync(string key, Stream source, long maximumBytes, CancellationToken ct) => throw new NotSupportedException();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Files;

namespace MeChat.Tests;

public sealed class FileRetentionIntegrationTests
{
    [Fact]
    public async Task Sql_preview_preserves_live_forward_avatar_and_expired_evidence_without_deleting_bytes()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(DatabaseTarget.Validate(DatabaseTarget.DefaultConnection)).Options);
        await db.Database.OpenConnectionAsync();
        await using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT DB_NAME()";
            Assert.Equal("MeChat", await command.ExecuteScalarAsync());
        }
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var suffix = userId.ToString("N");
        db.Users.Add(new User { Id = userId, Email = $"retention-{suffix}@example.invalid", NormalizedEmail = $"RETENTION-{suffix}@EXAMPLE.INVALID",
            Username = $"ret{suffix}", NormalizedUsername = $"RET{suffix}", DisplayName = "Synthetic retention test", PasswordHash = "No-login-test-only", CreatedAt = now });
        await db.SaveChangesAsync();
        StoredFile NewFile(string status, DateTime created) => new() { Id = Guid.NewGuid(), StorageKey = Guid.NewGuid().ToString("N"),
            OriginalFileName = "synthetic.txt", ContentType = "text/plain", SizeBytes = 1, UploadedByUserId = userId,
            Purpose = "Message", Status = status, CreatedAt = created };
        var pending = NewFile("Pending", now.AddDays(-2));
        var recent = NewFile("Pending", now);
        var recalled = NewFile("Attached", now.AddDays(-60));
        var forwarded = NewFile("Attached", now.AddDays(-60));
        var evidence = NewFile("Attached", now.AddDays(-60));
        var avatar = NewFile("Pending", now.AddDays(-2));
        db.StoredFiles.AddRange(pending, recent, recalled, forwarded, evidence, avatar);
        var conversationId = Guid.NewGuid();
        db.Conversations.Add(new Conversation { Id = conversationId, Type = "Private", Status = "Active", LastSequence = 4, CreatedAt = now, UpdatedAt = now });
        var source = new Message { Id = Guid.NewGuid(), ConversationId = conversationId, SenderUserId = userId,
            ClientMessageId = Guid.NewGuid(), Sequence = 1, RecalledAt = now.AddDays(-31), CreatedAt = now.AddDays(-40) };
        var forward = new Message { Id = Guid.NewGuid(), ConversationId = conversationId, SenderUserId = userId,
            ClientMessageId = Guid.NewGuid(), Sequence = 2, Content = "live forward", IsForwarded = true, CreatedAt = now };
        db.Messages.AddRange(source, forward);
        db.MessageAttachments.AddRange(
            new MessageAttachment { Id = Guid.NewGuid(), MessageId = source.Id, StoredFileId = recalled.Id, DisplayOrder = 0 },
            new MessageAttachment { Id = Guid.NewGuid(), MessageId = source.Id, StoredFileId = forwarded.Id, DisplayOrder = 1 },
            new MessageAttachment { Id = Guid.NewGuid(), MessageId = source.Id, StoredFileId = evidence.Id, DisplayOrder = 2 },
            new MessageAttachment { Id = Guid.NewGuid(), MessageId = forward.Id, StoredFileId = forwarded.Id, DisplayOrder = 0 });
        var reportId = Guid.NewGuid();
        db.Reports.Add(new Report { Id = reportId, ReporterUserId = userId, TargetType = "Message", TargetId = source.Id,
            Reason = "Synthetic retained evidence", Status = "Resolved", CreatedAt = now.AddDays(-150), ReviewedAt = now.AddDays(-100) });
        db.ReportEvidences.Add(new ReportEvidence { Id = Guid.NewGuid(), ReportId = reportId, SourceMessageId = source.Id,
            StoredFileId = evidence.Id, CapturedAt = now.AddDays(-150), RetainUntil = now.AddDays(-10) });
        await db.SaveChangesAsync();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(update => update.SetProperty(u => u.AvatarFileId, avatar.Id));
        var service = new FileRetentionService(db, new DenyStorage(), TimeProvider.System,
            Options.Create(new FileRetentionOptions { BatchSize = 500 }), NullLogger<FileRetentionService>.Instance);
        var preview = await service.PreviewAsync(CancellationToken.None);
        Assert.Contains(preview.Items, f => f.Id == pending.Id);
        Assert.Contains(preview.Items, f => f.Id == recalled.Id);
        foreach (var protectedId in new[] { recent.Id, forwarded.Id, evidence.Id, avatar.Id }) Assert.DoesNotContain(preview.Items, f => f.Id == protectedId);
        Assert.False(preview.DeletionEnabled);
        Assert.Equal(0, await service.SweepAsync(CancellationToken.None));
        Assert.False(await db.StoredFiles.AnyAsync(f => new[] { pending.Id, recalled.Id }.Contains(f.Id) && f.DeletedAt != null));
    }

    private sealed class DenyStorage : IFileStorage
    {
        public void Delete(string key) => throw new InvalidOperationException("Preview must not delete storage.");
        public Stream OpenRead(string key) => throw new NotSupportedException();
        public Task SaveAsync(string key, Stream source, long maximumBytes, CancellationToken ct) => throw new NotSupportedException();
    }
}

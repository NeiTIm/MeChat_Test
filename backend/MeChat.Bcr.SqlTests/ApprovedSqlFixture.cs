using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Features.Users.Models;
using MeChat.Api.Features.Notifications.Models;
using MeChat.Api.Features.Friends.Models;

namespace MeChat.Bcr.SqlTests;

// SOURCE ONLY IN G2. A future G3 approval must name the exact target and cleanup plan.
// Environment values are an operator interlock, never a substitute for PO permission.
public sealed class ApprovedSqlFixture
{
    private readonly string connection;
    public Guid A { get; } = Guid.NewGuid();
    public Guid B { get; } = Guid.NewGuid();
    public static readonly DateTime Now = new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

    public ApprovedSqlFixture()
    {
        connection = SqlTestTarget.Validate(
            Environment.GetEnvironmentVariable("MECHAT_BCR_G3_APPROVAL_REFERENCE"),
            Environment.GetEnvironmentVariable("MECHAT_BCR_SQL_ALLOWED_HOST"),
            Environment.GetEnvironmentVariable("MECHAT_BCR_SQL_DATABASE"),
            Environment.GetEnvironmentVariable("MECHAT_BCR_SQL_CONNECTION"));
    }

    // Test-only context construction. Production DI/DatabaseTarget remain unchanged.
    public AppDbContext Open() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);

    public async Task InitializeAsync()
    {
        await using var db = Open();
        // Existing operator-provisioned schema only. Never EnsureCreated/Migrate/seed an Admin.
        if (!(await db.Database.GetAppliedMigrationsAsync()).Contains("20261108144829_NotificationTrashFriendSeen"))
            throw new InvalidOperationException("BLOCKED: operator must provision and approve the BCR schema first.");
        db.Users.AddRange(User(A), User(B));
        await db.SaveChangesAsync();
    }

    private static User User(Guid id)
    {
        var name = "bcr" + id.ToString("N")[..20];
        return new() { Id = id, Email = name + "@example.test", NormalizedEmail = (name + "@example.test").ToUpperInvariant(),
            Username = name, NormalizedUsername = name.ToUpperInvariant(), DisplayName = "BCR test fixture", PasswordHash = "disabled-test-only",
            CreatedAt = Now.AddDays(-2), EmailVerifiedAt = Now.AddDays(-1), SystemRole = "User" };
    }

    public Notification Notice(bool read = false, bool trash = false, Guid? recipient = null) => new()
    {
        Id = Guid.NewGuid(), RecipientUserId = recipient ?? A, Type = "AdminWarning", CreatedAt = Now.AddDays(-1),
        ReadAt = read ? Now.AddMinutes(-1) : null, DeletedAt = trash ? Now.AddSeconds(-1) : null
    };

    public FriendRequest Request() => new()
    {
        Id = Guid.NewGuid(), SenderUserId = B, ReceiverUserId = A,
        PairUserId1 = A.CompareTo(B) < 0 ? A : B, PairUserId2 = A.CompareTo(B) < 0 ? B : A,
        CreatedAt = Now.AddDays(-1), Status = "Pending", SeenAt = null
    };
}

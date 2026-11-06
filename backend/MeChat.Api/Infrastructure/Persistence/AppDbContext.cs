using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence.Conventions;

namespace MeChat.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPrivacySetting> UserPrivacySettings => Set<UserPrivacySetting>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuthToken> AuthTokens => Set<AuthToken>();
    public DbSet<AccountSuspension> AccountSuspensions => Set<AccountSuspension>();
    public DbSet<AccountAppeal> AccountAppeals => Set<AccountAppeal>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<PrivateConversationPair> PrivateConversationPairs => Set<PrivateConversationPair>();
    public DbSet<PrivateChatAccess> PrivateChatAccess => Set<PrivateChatAccess>();
    public DbSet<MessageRequest> MessageRequests => Set<MessageRequest>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<ConversationUserState> ConversationUserStates => Set<ConversationUserState>();
    public DbSet<GroupProfile> GroupProfiles => Set<GroupProfile>();
    public DbSet<GroupInvitation> GroupInvitations => Set<GroupInvitation>();
    public DbSet<GroupInviteLink> GroupInviteLinks => Set<GroupInviteLink>();
    public DbSet<GroupPin> GroupPins => Set<GroupPin>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageEditHistory> MessageEditHistories => Set<MessageEditHistory>();
    public DbSet<MessageUserState> MessageUserStates => Set<MessageUserState>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<MessageDelivery> MessageDeliveries => Set<MessageDelivery>();
    public DbSet<ConversationReadCursor> ConversationReadCursors => Set<ConversationReadCursor>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportEvidence> ReportEvidences => Set<ReportEvidence>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        UtcDateTimeConvention.Apply(modelBuilder);
    }
}

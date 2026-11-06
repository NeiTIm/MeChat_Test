IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Conversations] (
    [Id] uniqueidentifier NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [LastSequence] bigint NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(320) NOT NULL,
    [NormalizedEmail] nvarchar(450) NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [NormalizedUsername] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [DisplayName] nvarchar(100) NOT NULL,
    [Bio] nvarchar(max) NULL,
    [AvatarFileId] uniqueidentifier NULL,
    [SystemRole] nvarchar(max) NOT NULL,
    [EmailVerifiedAt] datetime2 NULL,
    [LastUsernameChangedAt] datetime2 NULL,
    [DeactivatedAt] datetime2 NULL,
    [LastSeenAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [ConversationReadCursors] (
    [ConversationId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [LastReadSequence] bigint NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ConversationReadCursors] PRIMARY KEY ([ConversationId], [UserId]),
    CONSTRAINT [FK_ConversationReadCursors_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id])
);

CREATE TABLE [GroupInvitations] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [InviterUserId] uniqueidentifier NOT NULL,
    [InviteeUserId] uniqueidentifier NOT NULL,
    [InviteLinkId] uniqueidentifier NULL,
    [Status] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [RespondedAt] datetime2 NULL,
    CONSTRAINT [PK_GroupInvitations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupInvitations_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id])
);

CREATE TABLE [GroupProfiles] (
    [ConversationId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [AvatarFileId] uniqueidentifier NULL,
    [CreatedByUserId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_GroupProfiles] PRIMARY KEY ([ConversationId]),
    CONSTRAINT [FK_GroupProfiles_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id])
);

CREATE TABLE [MessageRequests] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [SenderUserId] uniqueidentifier NOT NULL,
    [ReceiverUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RespondedAt] datetime2 NULL,
    CONSTRAINT [PK_MessageRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageRequests_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id])
);

CREATE TABLE [PrivateChatAccess] (
    [ConversationId] uniqueidentifier NOT NULL,
    [SenderUserId] uniqueidentifier NOT NULL,
    [ReceiverUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [AcceptedAt] datetime2 NULL,
    [RejectedAt] datetime2 NULL,
    [RevokedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PrivateChatAccess] PRIMARY KEY ([ConversationId], [SenderUserId], [ReceiverUserId]),
    CONSTRAINT [FK_PrivateChatAccess_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id])
);

CREATE TABLE [PrivateConversationPairs] (
    [ConversationId] uniqueidentifier NOT NULL,
    [UserId1] uniqueidentifier NOT NULL,
    [UserId2] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_PrivateConversationPairs] PRIMARY KEY ([ConversationId]),
    CONSTRAINT [FK_PrivateConversationPairs_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id])
);

CREATE TABLE [AccountSuspensions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [AdminId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [RevokedAt] datetime2 NULL,
    CONSTRAINT [PK_AccountSuspensions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountSuspensions_Users_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_AccountSuspensions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [AdminAuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [AdminUserId] uniqueidentifier NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [TargetType] nvarchar(max) NOT NULL,
    [TargetId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Details] nvarchar(max) NULL,
    [LegalHold] bit NOT NULL,
    CONSTRAINT [PK_AdminAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdminAuditLogs_Users_AdminUserId] FOREIGN KEY ([AdminUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [AuthTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [TokenHash] nvarchar(450) NOT NULL,
    [PendingEmail] nvarchar(max) NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [UsedAt] datetime2 NULL,
    CONSTRAINT [PK_AuthTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuthTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [ConversationMembers] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [JoinSequence] bigint NOT NULL,
    [LeaveSequence] bigint NULL,
    [JoinedAt] datetime2 NOT NULL,
    [LeftAt] datetime2 NULL,
    [LeftReason] nvarchar(max) NULL,
    CONSTRAINT [PK_ConversationMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ConversationMembers_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]),
    CONSTRAINT [FK_ConversationMembers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [ConversationUserStates] (
    [ConversationId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [HiddenThroughSequence] bigint NOT NULL,
    [IsHidden] bit NOT NULL,
    [MutedUntil] datetime2 NULL,
    [RequestReadSequence] bigint NOT NULL,
    [RequestReadAt] datetime2 NULL,
    CONSTRAINT [PK_ConversationUserStates] PRIMARY KEY ([ConversationId], [UserId]),
    CONSTRAINT [FK_ConversationUserStates_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]),
    CONSTRAINT [FK_ConversationUserStates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [FriendRequests] (
    [Id] uniqueidentifier NOT NULL,
    [SenderUserId] uniqueidentifier NOT NULL,
    [ReceiverUserId] uniqueidentifier NOT NULL,
    [PairUserId1] uniqueidentifier NOT NULL,
    [PairUserId2] uniqueidentifier NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RespondedAt] datetime2 NULL,
    CONSTRAINT [PK_FriendRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FriendRequests_Users_ReceiverUserId] FOREIGN KEY ([ReceiverUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_FriendRequests_Users_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Friendships] (
    [Id] uniqueidentifier NOT NULL,
    [UserId1] uniqueidentifier NOT NULL,
    [UserId2] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RemovedAt] datetime2 NULL,
    CONSTRAINT [PK_Friendships] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Friendships_Users_UserId1] FOREIGN KEY ([UserId1]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Friendships_Users_UserId2] FOREIGN KEY ([UserId2]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Messages] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [SenderUserId] uniqueidentifier NOT NULL,
    [ClientMessageId] uniqueidentifier NOT NULL,
    [Sequence] bigint NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NULL,
    [ReplyToMessageId] uniqueidentifier NULL,
    [IsForwarded] bit NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [EditedAt] datetime2 NULL,
    [RecalledAt] datetime2 NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]),
    CONSTRAINT [FK_Messages_Users_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientUserId] uniqueidentifier NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [Type] nvarchar(max) NOT NULL,
    [ReferenceId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Reports] (
    [Id] uniqueidentifier NOT NULL,
    [ReporterUserId] uniqueidentifier NOT NULL,
    [TargetType] nvarchar(max) NOT NULL,
    [TargetId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReviewedByAdminId] uniqueidentifier NULL,
    [ReviewedAt] datetime2 NULL,
    CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reports_Users_ReporterUserId] FOREIGN KEY ([ReporterUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [StoredFiles] (
    [Id] uniqueidentifier NOT NULL,
    [StorageKey] nvarchar(450) NOT NULL,
    [OriginalFileName] nvarchar(max) NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [UploadedByUserId] uniqueidentifier NOT NULL,
    [Purpose] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_StoredFiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StoredFiles_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [UserBlocks] (
    [Id] uniqueidentifier NOT NULL,
    [BlockerUserId] uniqueidentifier NOT NULL,
    [BlockedUserId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UnblockedAt] datetime2 NULL,
    CONSTRAINT [PK_UserBlocks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserBlocks_Users_BlockedUserId] FOREIGN KEY ([BlockedUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_UserBlocks_Users_BlockerUserId] FOREIGN KEY ([BlockerUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [UserPrivacySettings] (
    [UserId] uniqueidentifier NOT NULL,
    [ShowOnline] bit NOT NULL,
    [ShowLastSeen] bit NOT NULL,
    [AllowStrangerMessages] bit NOT NULL,
    [AllowGroupInvites] bit NOT NULL,
    CONSTRAINT [PK_UserPrivacySettings] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserPrivacySettings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [UserSessions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [DeviceName] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastActivityAt] datetime2 NOT NULL,
    [RevokedAt] datetime2 NULL,
    CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [AccountAppeals] (
    [Id] uniqueidentifier NOT NULL,
    [SuspensionId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReviewedByAdminId] uniqueidentifier NULL,
    [ReviewedAt] datetime2 NULL,
    [ReviewNote] nvarchar(max) NULL,
    CONSTRAINT [PK_AccountAppeals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountAppeals_AccountSuspensions_SuspensionId] FOREIGN KEY ([SuspensionId]) REFERENCES [AccountSuspensions] ([Id]),
    CONSTRAINT [FK_AccountAppeals_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [GroupInviteLinks] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [CreatedByMembershipId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [RevokedAt] datetime2 NULL,
    CONSTRAINT [PK_GroupInviteLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupInviteLinks_ConversationMembers_CreatedByMembershipId] FOREIGN KEY ([CreatedByMembershipId]) REFERENCES [ConversationMembers] ([Id]),
    CONSTRAINT [FK_GroupInviteLinks_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id])
);

CREATE TABLE [GroupPins] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [PinnedByUserId] uniqueidentifier NOT NULL,
    [PinnedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_GroupPins] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupPins_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]),
    CONSTRAINT [FK_GroupPins_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id])
);

CREATE TABLE [MessageDeliveries] (
    [MessageId] uniqueidentifier NOT NULL,
    [RecipientUserId] uniqueidentifier NOT NULL,
    [DeliveredAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MessageDeliveries] PRIMARY KEY ([MessageId], [RecipientUserId]),
    CONSTRAINT [FK_MessageDeliveries_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id])
);

CREATE TABLE [MessageEditHistories] (
    [Id] uniqueidentifier NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [Version] int NOT NULL,
    [PreviousContent] nvarchar(max) NOT NULL,
    [EditedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MessageEditHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageEditHistories_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id])
);

CREATE TABLE [MessageReactions] (
    [Id] uniqueidentifier NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Emoji] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MessageReactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageReactions_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id])
);

CREATE TABLE [MessageUserStates] (
    [MessageId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [DeletedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MessageUserStates] PRIMARY KEY ([MessageId], [UserId]),
    CONSTRAINT [FK_MessageUserStates_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id])
);

CREATE TABLE [ReportEvidences] (
    [Id] uniqueidentifier NOT NULL,
    [ReportId] uniqueidentifier NOT NULL,
    [SourceMessageId] uniqueidentifier NULL,
    [EvidenceContent] nvarchar(max) NULL,
    [StoredFileId] uniqueidentifier NULL,
    [CapturedAt] datetime2 NOT NULL,
    [RetainUntil] datetime2 NULL,
    [LegalHold] bit NOT NULL,
    CONSTRAINT [PK_ReportEvidences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReportEvidences_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([Id])
);

CREATE TABLE [MessageAttachments] (
    [Id] uniqueidentifier NOT NULL,
    [MessageId] uniqueidentifier NOT NULL,
    [StoredFileId] uniqueidentifier NOT NULL,
    [DisplayOrder] int NOT NULL,
    CONSTRAINT [PK_MessageAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MessageAttachments_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]),
    CONSTRAINT [FK_MessageAttachments_StoredFiles_StoredFileId] FOREIGN KEY ([StoredFileId]) REFERENCES [StoredFiles] ([Id])
);

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(450) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [RevokedAt] datetime2 NULL,
    [ReplacedByTokenId] uniqueidentifier NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_UserSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [UserSessions] ([Id])
);

CREATE INDEX [IX_AccountAppeals_SuspensionId] ON [AccountAppeals] ([SuspensionId]);

CREATE INDEX [IX_AccountAppeals_UserId] ON [AccountAppeals] ([UserId]);

CREATE INDEX [IX_AccountSuspensions_AdminId] ON [AccountSuspensions] ([AdminId]);

CREATE INDEX [IX_AccountSuspensions_UserId] ON [AccountSuspensions] ([UserId]);

CREATE INDEX [IX_AdminAuditLogs_AdminUserId] ON [AdminAuditLogs] ([AdminUserId]);

CREATE INDEX [IX_AdminAuditLogs_CreatedAt] ON [AdminAuditLogs] ([CreatedAt]);

CREATE UNIQUE INDEX [IX_AuthTokens_TokenHash] ON [AuthTokens] ([TokenHash]);

CREATE INDEX [IX_AuthTokens_UserId] ON [AuthTokens] ([UserId]);

CREATE UNIQUE INDEX [IX_ConversationMembers_ConversationId_UserId] ON [ConversationMembers] ([ConversationId], [UserId]) WHERE [LeftAt] IS NULL;

CREATE INDEX [IX_ConversationMembers_UserId] ON [ConversationMembers] ([UserId]);

CREATE INDEX [IX_ConversationUserStates_UserId] ON [ConversationUserStates] ([UserId]);

CREATE UNIQUE INDEX [IX_FriendRequests_PairUserId1_PairUserId2] ON [FriendRequests] ([PairUserId1], [PairUserId2]) WHERE [Status] = 'Pending';

CREATE INDEX [IX_FriendRequests_ReceiverUserId] ON [FriendRequests] ([ReceiverUserId]);

CREATE INDEX [IX_FriendRequests_SenderUserId] ON [FriendRequests] ([SenderUserId]);

CREATE UNIQUE INDEX [IX_Friendships_UserId1_UserId2] ON [Friendships] ([UserId1], [UserId2]) WHERE [RemovedAt] IS NULL;

CREATE INDEX [IX_Friendships_UserId2] ON [Friendships] ([UserId2]);

CREATE INDEX [IX_GroupInvitations_ConversationId] ON [GroupInvitations] ([ConversationId]);

CREATE INDEX [IX_GroupInvitations_InviteeUserId_Status_CreatedAt] ON [GroupInvitations] ([InviteeUserId], [Status], [CreatedAt]);

CREATE INDEX [IX_GroupInviteLinks_ConversationId] ON [GroupInviteLinks] ([ConversationId]);

CREATE INDEX [IX_GroupInviteLinks_CreatedByMembershipId] ON [GroupInviteLinks] ([CreatedByMembershipId]);

CREATE UNIQUE INDEX [IX_GroupInviteLinks_TokenHash] ON [GroupInviteLinks] ([TokenHash]);

CREATE UNIQUE INDEX [IX_GroupPins_ConversationId_MessageId] ON [GroupPins] ([ConversationId], [MessageId]);

CREATE INDEX [IX_GroupPins_MessageId] ON [GroupPins] ([MessageId]);

CREATE UNIQUE INDEX [IX_MessageAttachments_MessageId_DisplayOrder] ON [MessageAttachments] ([MessageId], [DisplayOrder]);

CREATE INDEX [IX_MessageAttachments_StoredFileId] ON [MessageAttachments] ([StoredFileId]);

CREATE UNIQUE INDEX [IX_MessageEditHistories_MessageId_Version] ON [MessageEditHistories] ([MessageId], [Version]);

CREATE UNIQUE INDEX [IX_MessageReactions_MessageId_UserId] ON [MessageReactions] ([MessageId], [UserId]);

CREATE INDEX [IX_MessageRequests_ConversationId] ON [MessageRequests] ([ConversationId]);

CREATE INDEX [IX_MessageRequests_ReceiverUserId_Status_CreatedAt] ON [MessageRequests] ([ReceiverUserId], [Status], [CreatedAt]);

CREATE UNIQUE INDEX [IX_Messages_ConversationId_Sequence] ON [Messages] ([ConversationId], [Sequence]);

CREATE UNIQUE INDEX [IX_Messages_SenderUserId_ClientMessageId] ON [Messages] ([SenderUserId], [ClientMessageId]);

CREATE INDEX [IX_Notifications_RecipientUserId_CreatedAt] ON [Notifications] ([RecipientUserId], [CreatedAt]);

CREATE UNIQUE INDEX [IX_PrivateConversationPairs_UserId1_UserId2] ON [PrivateConversationPairs] ([UserId1], [UserId2]);

CREATE INDEX [IX_RefreshTokens_SessionId] ON [RefreshTokens] ([SessionId]);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);

CREATE INDEX [IX_ReportEvidences_ReportId] ON [ReportEvidences] ([ReportId]);

CREATE INDEX [IX_Reports_ReporterUserId] ON [Reports] ([ReporterUserId]);

CREATE INDEX [IX_Reports_Status_CreatedAt] ON [Reports] ([Status], [CreatedAt]);

CREATE UNIQUE INDEX [IX_StoredFiles_StorageKey] ON [StoredFiles] ([StorageKey]);

CREATE INDEX [IX_StoredFiles_UploadedByUserId] ON [StoredFiles] ([UploadedByUserId]);

CREATE INDEX [IX_UserBlocks_BlockedUserId] ON [UserBlocks] ([BlockedUserId]);

CREATE UNIQUE INDEX [IX_UserBlocks_BlockerUserId_BlockedUserId] ON [UserBlocks] ([BlockerUserId], [BlockedUserId]) WHERE [UnblockedAt] IS NULL;

CREATE UNIQUE INDEX [IX_Users_NormalizedEmail] ON [Users] ([NormalizedEmail]);

CREATE UNIQUE INDEX [IX_Users_NormalizedUsername] ON [Users] ([NormalizedUsername]);

CREATE INDEX [IX_UserSessions_UserId] ON [UserSessions] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261028101015_InitialSchema', N'10.0.12');

COMMIT;
GO


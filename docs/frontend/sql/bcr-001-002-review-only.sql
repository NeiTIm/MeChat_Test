-- REVIEW ARTIFACT ONLY. G3/G4 NOT_AUTHORIZED. DO NOT EXECUTE.
-- Generated offline; requires approved exact isolated target and maintenance write quiescence.
BEGIN TRANSACTION;
ALTER TABLE [Notifications] ADD [DeletedAt] datetime2(7) NULL;

ALTER TABLE [FriendRequests] ADD [SeenAt] datetime2(7) NULL;

EXEC(N'
    DECLARE @seenAt datetime2(7) = SYSUTCDATETIME();
    UPDATE [FriendRequests]
    SET [SeenAt] = @seenAt
    WHERE [Status] = N''Pending'' AND [SeenAt] IS NULL;
');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261108144829_NotificationTrashFriendSeen', N'10.0.12');

COMMIT;
GO


BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [FailedLoginCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Users] ADD [LockoutUntil] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261028101250_AuthLockoutFields', N'10.0.12');

COMMIT;
GO


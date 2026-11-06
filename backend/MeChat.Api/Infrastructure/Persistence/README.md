# EF Core persistence

`AppDbContext` uses `ApplyConfigurationsFromAssembly` for 32 dedicated `IEntityTypeConfiguration<T>` classes. `UtcDateTimeConvention` retains the original `DateTime` converter and global `DeleteBehavior.NoAction` configuration.

The original migration files retain MigrationIds 20261028101015_InitialSchema and 20261028101250_AuthLockoutFields. The C# namespace and fully-qualified CLR entity names have been updated so that EF Core can resolve types after moving them. Their SQL `Up`/`Down` steps are intentionally unaltered. The restore of `MeChat` should retain the corresponding `__EFMigrationsHistory` rows; this must be verified against the user's restored database.

Do not create/apply a migration merely for the refactor. Snapshot/model comparison requires .NET SDK, EF CLI and appropriate local configuration.

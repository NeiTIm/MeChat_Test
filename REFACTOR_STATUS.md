# MeChat refactor status — scope and caveats

## Delivered changes
- Renamed solution/projects C# namespaces to MeChat, DB target to `MeChat` on `lpc:.\MSSQLSERVER01`.
- Reorganized existing Feature controllers, DTOs, Services under Feature-first directories (original C# namespace per Feature preserved for backward internal references).
- Split exactly **32 entity classes** into Feature `Models/*.cs`.
- Split exactly **32 EF Core mappings** into individual `IEntityTypeConfiguration<T>` in Infrastructure/Persistence/Configurations/{Feature}; centralized UTC/delete behavior convention.
- Preserved both original MigrationIds, migration SQL operations, and converted snapshot/Designer CLR entity names and namespaces to correspond to their new C# locations.
- Replaced direct `IHubContext<ChatHub>` injection in existing Services with `IRealtimePublisher` / SignalRRealtimePublisher; existing event names and payloads retained.
- Added `IPrivateChatRepository` / `PrivateChatRepository`, moved several private-chat authorization read queries, introduced `IUnitOfWork` / `EfUnitOfWork` and applied it to private-chat transaction/save/clear usage.
- No restored database was modified and no EF migration command was executed.

## NOT finished / acceptance gaps
- **Repository architecture is incomplete.** Most existing Services still use DbContext/EF Core directly, and most Features do not yet have concrete Repositories/Interfaces or Services/Interfaces. The complete Feature-First + Layered pattern previously requested has **not** been achieved. New folder locations alone are not sufficient to claim a completed Repository Layer.
- Full decomposition of large Services by use case/responsibility is not complete.
- Build and tests were **not run**: .NET SDK unavailable in this environment. Exact source compatibility must be verified with `dotnet build MeChat.slnx`.
- Live database restore state, `__EFMigrationsHistory` and pending-model-change checks cannot be verified from the ZIP. No operations were performed against SQL Server.
- Existing `webchat_*` cookie names remain for backward-compatible HTTP contracts, and unchanged UserSecretsId retains local configuration. Any later renaming must coordinate with clients/secrets.
- Existing JWT issuer/audience changed to MeChat; old JWTs may require fresh login.
- Frontend and original `docs/spec` were absent from source ZIP and have not been recreated.

## Next work before calling this a complete refactor
1. On user's machine compile and resolve any problems; check EF model vs historical snapshot. Do not apply migrations without explicit approval.
2. Move each remaining Service's EF queries and mutations into Feature-specific repositories, while keeping business decisions and transaction boundaries at Service/UoW layer.
3. Introduce Service interfaces where meaningful and register them via dependency injection.
4. Recheck atomic message send, idempotency, authorization and SignalR fanout; build-only acceptance does not guarantee runtime equivalence.

## Source safety
The archive excludes `.git`, build output, old test results and private uploads/dev mail. It does not remove those files from the user's local computer.

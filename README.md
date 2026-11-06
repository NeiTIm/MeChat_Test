# MeChat — Backend refactor source

ASP.NET Core Web API (.NET 10), EF Core 10 / SQL Server, JWT, SignalR.

**Scope of this archive:** `backend/MeChat.Api`, `backend/MeChat.Tests`, scripts and tools available in the supplied source ZIP. The source ZIP did **not** contain a frontend or `docs/spec`; those are not part of this deliverable.

**Database safety:** application and design-time target `MeChat` on `lpc:.\MSSQLSERVER01` via Windows Authentication. This refactor performs no SQL operations on the user's actual restored database. No migration has been applied. The historical two MigrationIds are preserved.

**Development prerequisites:** Windows, .NET SDK 10, SQL Server named instance, valid JWT secret already registered under the unchanged UserSecretsId. Config file: `backend/MeChat.Api/appsettings.Development.json`.

```powershell
dotnet restore MeChat.slnx
dotnet build MeChat.slnx
dotnet ef migrations list --project backend/MeChat.Api --startup-project backend/MeChat.Api
dotnet ef migrations has-pending-model-changes --project backend/MeChat.Api --startup-project backend/MeChat.Api
```

Do **not** run `dotnet ef database update`, drop, or re-create migrations during this rename. Never connect to the old database.

See `REFACTOR_STATUS.md` for achieved changes and incomplete architecture work. Build/tests **have not been executed** in the packaging environment (no .NET SDK). This package must not be treated as build-verified or migration-verified until validated on the user's machine.

Compatibility: HTTP routes and SignalR event names remain unchanged. Existing cookie names `webchat_refresh` and `webchat_csrf` remain unchanged to preserve client contracts. UserSecretsId also remains unchanged to retain the existing local secrets store. JWT issuer/audience are renamed to `MeChat.Api` / `MeChat.Frontend`, so tokens from the old issuer require re-login.

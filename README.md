# MeChat

ASP.NET Core Web API (.NET 10), EF Core 10 / SQL Server, JWT, SignalR.

ASP.NET Core backend and React/Vite JavaScript frontend for a portfolio chat application. The visual baseline is approved. Client features are implemented and undergoing the [workbook improvement pass](docs/frontend/improvement-progress.md); authenticated integration is not yet accepted.

## Frontend preview

Requires Node.js 22.12+ and npm. On Windows PowerShell use `npm.cmd` if execution policy blocks `npm.ps1`.

```powershell
cd frontend
npm.cmd ci
npm.cmd run dev
```

Open http://127.0.0.1:5173. Development-only design samples:

- [Login](http://127.0.0.1:5173/design/login)
- [Private chat](http://127.0.0.1:5173/design/private)
- [Group chat](http://127.0.0.1:5173/design/group)

The official visual direction is **MeChat Hybrid**. Product Owner approved Option C and all three screen baselines on 2026-11-06: **APPROVED: DESIGN_APPROVAL_GRANTED_BY_PRODUCT_OWNER**. All User/Admin UI is Vietnamese-first (vi-VN). Visual approval does not accept unverified functionality. The revised [design review and advisory scorecards](docs/frontend/design-review.md) and [full screenshot index](docs/frontend/screenshot-index.md) cover 5 viewport widths in both themes. Sample message actions, receipts, pins, role and invitation controls are explicitly labeled local previews; they are not accepted live features.

Samples are clearly labeled fixtures and do not call authentication/chat APIs. They are excluded from the production build. The live `/login`, `/chat` and `/groups` routes use real API adapters. Auth/account, Friends, private messages/requests, Groups, Files/Avatar, Notifications, Reports/Appeals and Admin now have real API consumers. Their successful business integration remains NEEDS_VERIFICATION; HTTP client simulations and design approval do not accept Backend journeys.

Start the existing backend from the repository root; this keeps retention jobs and deletion disabled for local preview without modifying backend configuration:

```powershell
dotnet run --project backend/MeChat.Api --no-build -- --FileRetention:RunScheduled=false --FileRetention:EnableDeletion=false --RetentionMaintenance:RunScheduled=false --RetentionMaintenance:EnablePurge=false
```

Vite proxies `/api` and `/hubs` (WebSocket enabled) to http://localhost:5080. Override server-only `API_PROXY_TARGET` using an ignored local `.env` if needed. Use the same frontend origin consistently so Strict cookies remain available. Production hosting needs a same-origin reverse proxy and SPA fallback; `vite preview` is not a production API proxy.

```powershell
cd frontend
npm.cmd run build
npm.cmd run lint
npm.cmd test
npx.cmd playwright install chromium
npm.cmd run test:e2e
npm.cmd run test:e2e:live
```

The live suite checks anonymous API boundaries. Its account test skips unless `MECHAT_E2E_IDENTIFIER` and `MECHAT_E2E_PASSWORD` are supplied securely in the process environment. Never put credentials in source, logs, screenshots, reports or Git. No credentials are read from private uploads/dev-mail.

Responsive layout is preparation for a future PWA. This build has no service worker, installable PWA guarantee or offline cache of private messages.

Progress/recovery: [roadmapFE.md](roadmapFE.md). Contracts: [API inventory](docs/frontend/api-inventory.md), [feature matrix](docs/frontend/api-to-feature-map.md). Design evidence: [UI review](docs/frontend/ui-ux-acceptance.md). Current limitations: [acceptance report](docs/frontend/final-acceptance.md).

## Existing backend

**Database safety:** application and design-time target `MeChat` on `lpc:.\MSSQLSERVER01` via Windows Authentication. This refactor performs no SQL operations on the user's actual restored database. No migration has been applied. The historical two MigrationIds are preserved.

**Development prerequisites:** Windows, .NET SDK 10, SQL Server named instance, valid JWT secret already registered under the unchanged UserSecretsId. Config file: `backend/MeChat.Api/appsettings.Development.json`.

```powershell
dotnet restore MeChat.slnx
dotnet build MeChat.slnx
dotnet ef migrations list --project backend/MeChat.Api --startup-project backend/MeChat.Api
dotnet ef migrations has-pending-model-changes --project backend/MeChat.Api --startup-project backend/MeChat.Api
```

Do **not** run `dotnet ef database update`, drop, or re-create migrations during this rename. Never connect to the old database.

The original source archive did not include a frontend. Historical packaging notes did not establish build/migration verification. Frontend work does not claim that all backend journeys are tested; evidence is tracked separately in the frontend reports.

Compatibility: HTTP routes and SignalR event names remain unchanged. Existing cookie names `webchat_refresh` and `webchat_csrf` remain unchanged to preserve client contracts. UserSecretsId also remains unchanged to retain the existing local secrets store. JWT issuer/audience are renamed to `MeChat.Api` / `MeChat.Frontend`, so tokens from the old issuer require re-login.

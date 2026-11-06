# Frontend architecture

Feature-first JavaScript/JSX. `app` owns routing/providers; `features` owns API modules and screens; `components/ui` contains shadcn primitives; `components/chat` contains reusable visual workspace; `lib` owns HTTP/query/error/session/realtime infrastructure; `stores` contains memory auth and persisted theme only. Create feature directories when functionality exists, not empty scaffolding.

Normal chat uses one Axios instance, credentials enabled, relative `/api` base. Appeals uses a separate transport/token/local cache scope and never installs its token in normal auth. Session manager owns CSRF refresh, bounded 401 recovery, timer-based token refresh through access factory, cookie-rotation lock and session loss callback. Auth store never persists identity/tokens. Query cache is cleared on logout/account switch. Public requests opt out of auth refresh. Router guards await session recovery and profile-based role guards; UI roles never replace backend checks.

One SignalR provider per authenticated app; no fixture provider in production. Use ID events to invalidate real HTTP queries. Remove sensitive resource caches on permission/membership changes. Reconnect and foreground periodic resync recover missed events. Cleanup on identity change and unmount. Full multi-tab/session/realtime acceptance remains a later test gate.

Design review routes dynamically import an isolated module only when `import.meta.env.DEV` is true. Review data lives there and is visibly labeled. The production bundle must be scanned to confirm fixture strings/review modules are absent.

Vitest/RTL verify validation, error normalization, session recovery and critical render behavior. Playwright verifies sample interactions, both themes, responsive layout, axe checks and screenshots; a separate environment-driven live suite never embeds credentials and does not save authenticated traces/screenshots. Build + test evidence is versioned in docs; generated reports/caches are ignored.

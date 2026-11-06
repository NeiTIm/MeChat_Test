# Verification evidence at design checkpoint

## Current Hybrid review revision

Product Owner confirmed Visual Direction C while explicitly keeping screen approval BLOCKED. Current UI verification: build/lint/production fixture scan PASS, **19 unit/component tests PASS**, **49 browser cases PASS** (30 screen/width/theme combinations plus states/interactions), audit 0 vulnerabilities. [Current detailed review](design-review.md), [per-case summary](hybrid-test-summary.json), [screenshot index](screenshot-index.md). Physical mobile/native autofill/screen readers/native browsers and real authenticated two-user integration remain NEEDS_VERIFICATION. No backend or database change. The evidence below records the earlier foundation checkpoint; its counts are historical, not the current UI suite size.

## Earlier foundation checkpoint

2026-11-06, Asia/Bangkok. Story: a user enters MeChat through login, obtains a cookie-backed session and opens a private/group conversation; before implementing the full story, three representative designs must be approved.

| Boundary | Result | Actual evidence |
| --- | --- | --- |
| Design UI renders | PASS | 12 Chromium sample combinations + screenshots; no page errors |
| Form / component behavior | PASS | RHF/Zod field errors, password visibility, plain-text message rendering, failed-draft retention/idempotent retry |
| Refresh concurrency / stale response | PASS (unit) | Concurrent refresh single-flight, current CSRF lookup, protected 401 replay once, rejected refresh clears identity, logout-racing-refresh cannot restore identity |
| Browser → Vite → real backend | PASS (anonymous) | `/api/health` 200 `{status:"ok",service:"MeChat.Api"}` |
| Auth validation / protected boundary | PASS (negative) | Empty login 400; missing refresh CSRF 401 INVALID_REFRESH; users/me, conversations, groups, admin/dashboard 401 |
| Hub HTTP proxy | PASS (anonymous rejection) | POST `/hubs/chat/negotiate?negotiateVersion=1` 401 |
| Authenticated WebSocket | NOT TESTED | `ws:true` configured, but no successful authenticated socket tested |
| API → SQL → successful auth/chat response | NOT TESTED | No environment credentials supplied; stop full-story acceptance at this boundary |
| Response → real populated chat UI | NOT TESTED | Source-correct minimal adapters exist; design fixtures are separate evidence only |

Commands from frontend:

```powershell
npm.cmd run build
npm.cmd test
npm.cmd run test:e2e
npm.cmd run test:e2e:live
npm.cmd run verify:production
npm.cmd audit
```

At submission: Vitest 17 passed; design E2E 18 passed; live boundary suite 3 passed / account journey 1 skipped. Build and production-isolation scan pass. Screenshots are saved under docs/frontend/screenshots; generated Playwright reports are ignored. Browser was Playwright Chromium 158.0.8127.18. Node 22.14.0, npm 10.9.2, Vite 7.3.7, React 19.3.1, Vitest 5.1.0.

Manual inspection covered all 12 theme/viewport screenshots plus dialog/list/focus/validation. No comprehensive screen-reader, real keyboard, native Edge/Chrome release, browser zoom or mobile device acceptance is implied. Re-run only affected checks after revisions, then freeze regression baselines after approval.

# Phase 4 — Structural refactoring

Date: 2026-11-09. Status: COMPLETED (structural refactoring and available client verification). No commit/push, Backend or database changes.

The source baseline was copied outside the workspace before edits. Existing Phase 2 formatting (47 modified JSX files and two untracked Prettier configuration files) and Phase 3 Conversations cleanup are preserved.

| Order | Feature | Status | Changes / verification |
| --- | --- | --- | --- |
| 1 | Shared Chat | COMPLETED | Extract ConversationList, ThreadHeader, ConversationDetails. Parent retains state, refs, effects and read/scroll ownership. Prettier, ESLint, build, 7 unit/component tests and 60 isolated browser tests PASS. Three extracted render trees compile identically to baseline. |
| 2 | Conversations | COMPLETED | Extract useConversationHistory, useConversationTyping, RequestActions and mergeMessages. Preserve query options and Join/StopTyping/Leave timing. Prettier, ESLint, build, 27 targeted tests and 4 isolated browser tests PASS. |
| 3 | Moderation & Appeals | COMPLETED | Move reusable ReportForm/ReportDialog out of page module; update consumers. Appeal transport/session untouched. Prettier/lint/build, 13 unit tests and 2 browser tests PASS. |
| 4 | Messages | COMPLETED | Extract MessageMetadata and MessageActionDialog. Mutation and dialog state remain in original owner. Prettier/lint/build, 18 unit tests and 3 browser tests PASS. |
| 5 | Files & Avatar | COMPLETED | Extract useBlobUrl and AttachmentDraft. Avatar and editor no longer import preview UI. Prettier/lint/build, 5 unit tests and 3 browser tests PASS. |
| 6 | Friends | COMPLETED | Extract PublicProfile, FriendRow, UserSearch; preserve seen queue. Prettier/lint/build, 17 unit tests and 1 browser test PASS. |
| 7 | Groups | COMPLETED | Extract invite form, members, invite links and membership actions; keep one-time link state in management route. Prettier/lint/build, 7 unit tests and 4 browser tests PASS. |
| 8 | Notifications | COMPLETED | Extract NotificationCard and ConversationNotifications; preserve public exports and feed/target/lifecycle hooks. Prettier/lint/build, 16 unit tests and 2 browser tests PASS. |
| 9 | Users & Settings | COMPLETED | Extract SettingsCard, ProfileSettings, PrivacySettings, SecuritySettings, SessionSettings. Prettier/lint/build, 12 unit tests and 2 browser tests PASS. |
| 10 | Admin | COMPLETED | Separate dashboard, list, record, case and retention views with shared MetricCards. Prettier/lint/build, 5 unit tests and 4 browser tests PASS. |
| 11 | Authentication | KEPT AS-IS | Keep API facade/session transport, schemas and forms. Format-only JS changes preserve AST. Prettier/lint/build, 13 unit tests and 2 browser tests PASS. |
| 12 | Shared Layout & Forms | COMPLETED | Extract useNavigationCounts behind the existing authenticated/preview boundary. Keep forms cohesive. Prettier/lint/build, 12 unit tests and 2 browser tests PASS. |
| 13 | App & Realtime | KEPT AS-IS | Single connection owner already delegates message hints; keep providers/guards/router and lifecycle unchanged. Prettier/lint/build, 18 unit tests and 2 browser tests PASS. |
| 14 | Shared UI & Feedback | KEPT AS-IS | Existing primitives and toast lifecycle are cohesive; no speculative hooks. Prettier/lint/build, 5 unit tests and 2 browser tests PASS. |
| 15 | Foundation & Stores | KEPT AS-IS | Preserve boundaries, session transport, stores and query defaults. Normalize existing JS formatting only, with AST equivalence checks. Full 81 unit tests and 3 request-storm/reconnect/429 browser tests PASS; Prettier/lint/build PASS. |
| 16 | Design Preview | COMPLETED | Extract fixture data/helper from review UI, keep DEV-only boundary. Initial browser run: 3 FAIL due to missing sampleMessage export; restored required export, added named-export validation. Fresh targeted run: 3 PASS; Prettier/lint/build, 7 unit tests and production fixture exclusion PASS. |

Final import graph: 120 JS/JSX modules, no broken literal internal imports, missing named exports, unreachable modules or circular dependencies. Git diff whitespace check PASS. Browser checks use HTTP/WebSocket simulation and an unreachable API proxy; they do not verify live SQL/API integration.

## Recovery / Next Exact Action

All 16 feature groups have been processed: 12 COMPLETED, 4 KEPT AS-IS structurally. No PARTIAL or BLOCKED refactor item remains in the approved scope. No next feature is queued. Next Exact Action: manual integration acceptance with authorized Development accounts and native devices; live integration remains NEEDS_VERIFICATION. Per-feature counts overlap; do not sum them into unique-case totals. Three new typing hook tests cover rerender deduplication, throttle/room cleanup, request-mode suppression and reconnect.


## Scope and file inventory

All 85 original JS/JSX modules were covered by the approved audit and checked against the current source. After extraction: 120 JS/JSX modules (77 JSX, 43 JS), 135 total source files. All 120 modules are reachable from main, including DEV-gated imports; no broken internal imports, missing named exports or circular dependencies were found. Reachability does not prove every export is used.

Phase 4 source changes: 35 added, 49 modified (15 JSX structural/import changes, 34 JS format-only), zero deleted or moved files. Additional changes: one new typing test, this report, roadmapFE.md. Existing Prettier config files belong to Phase 2, not newly added by Phase 4. CSS and all locale JSON are byte-identical to the source baseline.

### Added source modules

- `frontend/src/design/fixtures.js`
- `frontend/src/components/chat/ConversationDetails.jsx`
- `frontend/src/components/chat/ConversationList.jsx`
- `frontend/src/components/chat/ThreadHeader.jsx`
- `frontend/src/components/layout/hooks/use-navigation-counts.js`
- `frontend/src/features/admin/components/AdminCase.jsx`
- `frontend/src/features/admin/components/AdminDashboard.jsx`
- `frontend/src/features/admin/components/AdminList.jsx`
- `frontend/src/features/admin/components/AdminRecord.jsx`
- `frontend/src/features/admin/components/AdminRetention.jsx`
- `frontend/src/features/admin/components/MetricCards.jsx`
- `frontend/src/features/conversations/components/RequestActions.jsx`
- `frontend/src/features/conversations/hooks/use-conversation-history.js`
- `frontend/src/features/conversations/hooks/use-conversation-typing.js`
- `frontend/src/features/conversations/utils/merge-messages.js`
- `frontend/src/features/files/components/AttachmentDraft.jsx`
- `frontend/src/features/files/hooks/use-blob-url.js`
- `frontend/src/features/friends/components/FriendRow.jsx`
- `frontend/src/features/friends/components/PublicProfile.jsx`
- `frontend/src/features/friends/components/UserSearch.jsx`
- `frontend/src/features/groups/components/GroupDanger.jsx`
- `frontend/src/features/groups/components/GroupInviteForm.jsx`
- `frontend/src/features/groups/components/GroupLinks.jsx`
- `frontend/src/features/groups/components/GroupMembers.jsx`
- `frontend/src/features/messages/components/MessageActionDialog.jsx`
- `frontend/src/features/messages/components/MessageMetadata.jsx`
- `frontend/src/features/moderation/components/ReportDialog.jsx`
- `frontend/src/features/moderation/components/ReportForm.jsx`
- `frontend/src/features/notifications/components/ConversationNotifications.jsx`
- `frontend/src/features/notifications/components/NotificationCard.jsx`
- `frontend/src/features/users/components/PrivacySettings.jsx`
- `frontend/src/features/users/components/ProfileSettings.jsx`
- `frontend/src/features/users/components/SecuritySettings.jsx`
- `frontend/src/features/users/components/SessionSettings.jsx`
- `frontend/src/features/users/components/SettingsCard.jsx`

### Modified JSX modules

- `frontend/src/components/Avatar.jsx`
- `frontend/src/design/DesignReview.jsx`
- `frontend/src/components/chat/ChatWorkspace.jsx`
- `frontend/src/components/chat/Composer.jsx`
- `frontend/src/components/layout/NavigationLinks.jsx`
- `frontend/src/features/admin/AdminPage.jsx`
- `frontend/src/features/conversations/WorkspacePage.jsx`
- `frontend/src/features/files/AttachmentPreview.jsx`
- `frontend/src/features/files/AvatarEditor.jsx`
- `frontend/src/features/friends/FriendsPage.jsx`
- `frontend/src/features/groups/GroupPages.jsx`
- `frontend/src/features/messages/MessageActions.jsx`
- `frontend/src/features/moderation/ReportsPage.jsx`
- `frontend/src/features/notifications/NotificationsPage.jsx`
- `frontend/src/features/users/AccountPage.jsx`

### Existing JS modules: formatting only

- `frontend/src/hooks/use-wide-layout.js`
- `frontend/src/i18n/format.js`
- `frontend/src/i18n/index.js`
- `frontend/src/lib/errors.js`
- `frontend/src/lib/http.js`
- `frontend/src/lib/utils.js`
- `frontend/src/stores/theme-store.js`
- `frontend/src/stores/ui-feedback-store.js`
- `frontend/src/components/chat/use-thread-scroll.js`
- `frontend/src/components/layout/navigation.js`
- `frontend/src/features/admin/api.js`
- `frontend/src/features/admin/format.js`
- `frontend/src/features/auth/api.js`
- `frontend/src/features/auth/schemas.js`
- `frontend/src/features/conversations/api.js`
- `frontend/src/features/files/api.js`
- `frontend/src/features/files/use-attachment-draft.js`
- `frontend/src/features/files/validation.js`
- `frontend/src/features/friends/api.js`
- `frontend/src/features/friends/seen-queue.js`
- `frontend/src/features/friends/use-friend-unread.js`
- `frontend/src/features/friends/use-request-seen.js`
- `frontend/src/features/groups/api.js`
- `frontend/src/features/messages/api.js`
- `frontend/src/features/messages/cache.js`
- `frontend/src/features/messages/message-events.js`
- `frontend/src/features/messages/message-metadata.js`
- `frontend/src/features/messages/use-message-receipts.js`
- `frontend/src/features/messages/use-visible-read.js`
- `frontend/src/features/moderation/api.js`
- `frontend/src/features/notifications/api.js`
- `frontend/src/features/notifications/message-target.js`
- `frontend/src/features/notifications/use-notifications.js`
- `frontend/src/features/users/api.js`

## Responsibilities and preserved boundaries

- ConversationList, ThreadHeader, ConversationDetails: rendering only; ChatWorkspace retains filter/dialog state, refs, focus/jump, visible-read and scroll ownership. No added DOM wrapper.
- useConversationHistory: original infinite-query options, cursor, enabled condition and AbortSignal. useConversationTyping: original Join/StopTyping/Leave effect and 2500ms throttle, no new connection/subscription. RequestActions: existing request confirmations. mergeMessages: original pure dedupe/sequence helper.
- MessageMetadata: existing reply/reaction/receipt consumers. MessageActionDialog: dialog rendering; parent retains operation/destination/retry ID/mutation/history query and cache invalidation.
- ReportForm/ReportDialog: reusable reporting UI no longer imported through a page. Appeals transport, token scope, expiry and cleanup unchanged.
- useBlobUrl: allocation/revocation lifecycle shared by preview/avatar/editor. AttachmentDraft: draft UI, upload orchestration remains in existing hook.
- FriendRow/PublicProfile/UserSearch: existing row observer, profile dialog and paginated search boundaries; seen queue untouched.
- GroupInviteForm/GroupMembers/GroupLinks/GroupDanger: independent management areas. One-time created-link state stays in GroupManagementPage; existing refresh callback is passed to invite/member components.
- NotificationCard: presentation/actions with parent mutation state. ConversationNotifications: existing mute preferences, separate from feed page.
- Account setting components: profile, privacy, security and sessions; SettingsCard shared within feature. PrivacyForm state/initial values, session revocation and logout behavior retained.
- Admin views: dashboard, paginated list, record actions, report case and retention; MetricCards reused by dashboard/retention. No new retention operation.
- useNavigationCounts: existing server projections and query options behind existing authenticated/preview boundary. No duplicate server-state store.
- design/fixtures.js: sample data and history factory, imported only by DEV design UI. Production exclusion verified.

All 85 original module public exports remain available. Thirty moved function bodies compile identically to baseline. Five extracted JSX render subtrees compile identically. All 37 original JS module ASTs remain identical; 34 received formatting only. These static comparisons supplement runtime checks, not live integration acceptance.

## Remaining manual integration checks

NEEDS_VERIFICATION: real two-account JWT/refresh/CSRF/session recovery and switching; Private/Group message send/read/delivered/seen/typing/reconnect; pending-jump cancellation during actual network delays; member permission changes and one-time links; actual upload/authorized preview/download and avatar permissions; notification trash/restore and friend seen batches; reports/appeals/Admin authorization. Native mobile keyboards, screen reader and real-device focus restoration remain unverified. Browser HTTP/WebSocket simulation cannot establish Backend/SQL acceptance. No live mutations or database tests were executed.


## Final verification results

Commands ran from `frontend/`. No live mutation/SQL tests were run.

| Check | Actual result | Evidence |
| --- | --- | --- |
| Prettier | PASS: all 120 source JS/JSX modules and the new test | Existing VS Code bundled Prettier 3.7.4; no dependency installed |
| ESLint | PASS, zero warnings | `npm.cmd run lint` |
| Production build | PASS | `npm.cmd run build`; existing third-party PURE annotation warnings only |
| Unit/component | 81 PASS, 17 test files | `npm.cmd test`; includes 3 new typing hook regression tests |
| Full Chromium client/design regression | 191 PASS, 0 FAIL, 0 skipped, 0 flaky | `frontend/test-results/phase4-final-results.json` |
| BCR browser simulation | 14 PASS, 0 FAIL, 0 skipped, 0 flaky | `frontend/test-results/phase4-bcr-results.json` |
| Request storm/reconnect/429 | 3 PASS | `frontend/test-results/phase4-foundation/request-storm.json` |
| Production browser smoke | 1 PASS | `frontend/test-results/phase4-production-results.json` |
| Production design-fixture isolation | PASS | `npm.cmd run verify:production` |
| Localization | PASS: 695 static calls resolve | `npm.cmd run audit:localization` |
| API mapping | PASS: 128 actions mapped; static only | `frontend/test-results/phase4-verification/api-consumer-audit.json` |
| Imports/exports/cycles/reachability | PASS: 120 modules | Entire source import graph, including DEV modules |
| Source preservation | PASS | 85 public module interfaces, 37 original JS ASTs, 30 moved function bodies, 5 extracted render subtrees |
| Git diff / new-file whitespace | PASS | No Backend, dependency, tooling, locale or CSS changes |

The 191 client cases, 14 BCR cases, 3 request-storm cases and 1 production case are distinct suites. Per-feature targeted runs overlap these suites and are not added again. All browser acceptance here uses test fixtures/simulation or unauthenticated production routes, not live business integration. Full client regression includes all five viewport widths, both themes, keyboard/a11y checks, message targeting, paging, IME, media authorization failures, session and permission scenarios. Screenshots are generated by existing tests; no separate pixel-diff certification or native screen-reader acceptance was performed.

Request measurements (synthetic 30-message fixture, 1366x900): open 33 total / 8 context / 4 receipts; after 60s idle 56 total / 16 context / 8 receipts. Idle adds 23 total, including 12 metadata requests from existing reconciliation. Receipt/reaction hints leave history count unchanged (4 before/after hints); reconnect reloads history (5). A simulated 429 adds no receipt requests after hints. These numbers are a regression measurement, not a before/after live performance claim.

Existing API audit, performance and request-storm reports were preserved byte-for-byte; fresh results were copied into ignored test-results. Two tracked legacy PNGs regenerated by the suite were initially clean: fresh copies were preserved under `frontend/test-results/phase4-verification/screenshots/`, then original tracked bytes were reinstated. No user source changes were reset or restored.

## Issues and residual risk

- Fixed during this refactor: Design Preview needed sampleMessage exported for history generation. Initial targeted run recorded 3 FAIL; after the export fix, fresh targeted run 3 PASS and final full 191 PASS. Named-export checks now cover DEV imports that production tree-shaking excludes. Failure artifacts remain separate from successful reruns.
- Normalized documentation line endings after Git whitespace checks flagged CRLF; final diff check PASS.
- Refactor risk is concentrated in component mounting/focus and query/lifecycle ownership. Parent state/refs, keys and copied function bodies were retained; targeted and full client suites pass. Native focus restoration and real reconnect/account switching still require manual acceptance.
- No Backend, database, schema, business rule, API/SignalR contract, feature flag or dependency changes. No commit/push.

## Additional files and rerun commands

Added test: `frontend/tests/conversation-typing.test.jsx`. Added report: `docs/frontend/phase4-refactoring-progress.md`. Updated checkpoint: `roadmapFE.md`. Relative to the start-of-turn baseline, this task adds 37 files and modifies 50 files in total, with zero file moves/deletions; counts exclude pre-existing Phase 2 Prettier config files and generated test artifacts.

- Prettier: existing bundled CLI with `--check "src/**/*.{js,jsx}" tests/conversation-typing.test.jsx`.
- `npm.cmd run lint`; `npm.cmd run build`; `npm.cmd test`; `npm.cmd run audit:localization`; `npm.cmd run verify:production`.
- Full client: `npx.cmd playwright test --config playwright.g2-regression.config.js --output=test-results/phase4-final --reporter=line,json` (set `PLAYWRIGHT_JSON_OUTPUT_FILE` to a fresh result path).
- BCR: set `BCR_CAPTURE_SCREENSHOTS=false`, then `npx.cmd playwright test --config playwright.bcr.config.js --output=test-results/phase4-bcr --reporter=line,json`.
- Storm: `npx.cmd playwright test --config playwright.request-storm.config.js --output=test-results/phase4-foundation --reporter=line`; preserve historical docs output before rerunning.
- Production: set `API_PROXY_TARGET=http://127.0.0.1:9`, then `npx.cmd playwright test --config playwright.production.config.js --output=test-results/phase4-production --reporter=line,json`.

Existing screenshot/performance/audit test scripts write historical docs paths; preserve their current contents and copy new artifacts separately when rerunning. Test-only proxy/flag environment variables apply to isolated child servers, not the running Development app.

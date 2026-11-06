# Screen inventory and states

| Area | Screens / overlays | Gate scope | States required |
| --- | --- | --- | --- |
| Auth | Login; later register/verify/reset/reactivate | Login only | Idle, field errors, submitting, invalid credentials/state, offline, 429, retry |
| Inbox | List, private thread, details | Representative sample + minimal live adapter | Loading, empty, selected, error/retry, permission unavailable, sending/failure |
| Groups | List, group thread, members/details | Representative sample + minimal live adapter | Loading/empty/error, member role, access changed, sending |
| Message tools | Reply/edit/recall/reaction/forward/history/search/context | Later | Unavailable source, stale permission, validation, mutation errors |
| People | Search/profile, friends, requests, blocked | Later | Pagination, no results, privacy, conflict |
| Requests | Inbox/detail/respond | Later | Pending, rejected, accepted, access lost |
| Group management | Create/profile/invite/member roles/links/pins | Later | Confirmation, role visibility, invalid/expired links |
| Media | Upload tray, authorized preview/download | Later | Progress, validation, canceled/error, inaccessible |
| Notifications | List, unread, destination | Later | Loading/empty, read/unread, missing reference |
| Account | Profile/avatar/username/email/privacy/password/sessions | Later | Validation, cooldown, confirmation, revoked session |
| Trust | Report form/my reports; appeal authentication/my appeals | Later | Restricted identity, pending review, validation |
| Admin | Dashboard/lists/case/evidence/appeals/audit/retention | After user slices | Role forbidden, claim conflict, audit reason, read-only preview |

All implemented screens use shared state panels, labeled controls, theme tokens and responsive rules. Design fixtures are explicitly labeled; no fake users/messages enter live mode.

Hybrid review revision adds development-only previews within these three screens: recovery/register entry dialogs, message reply/edit/recall/reaction and receipts, pin/member/role/invitation interactions. These are explicitly labeled fixtures; the live feature rows above remain later slices, pending approval and real integration verification.

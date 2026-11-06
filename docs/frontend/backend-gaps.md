# Backend gaps and integration constraints

2026-11-06. Backend source is unchanged. No database actions authorized or performed.

| ID | Finding / source | Impact | Frontend response / proposal | Approval |
| --- | --- | --- | --- | --- |
| BG-001 | CORS allows localhost:5173, but omits AllowCredentials; auth uses Strict cookies | Direct cross-origin refresh/logout will fail | Same-origin Vite `/api` and `/hubs` proxies; deploy behind same-origin reverse proxy. Only propose CORS change if separate origins are required | No backend change needed for dev |
| BG-002 | ConversationDto contains otherUserId, no display name/avatar/last-message preview | Extra profile reads; do not invent preview text or unread counts | Resolve profiles through Users API; show explicit neutral list metadata until loaded; future summary projection is optional | Deferred; no change requested |
| BG-003 | SessionConnections.Revoke aborts connections. SessionRevoked is emitted on suspension, not every logout/revoke path | Cannot rely on one event for revocation | HTTP 401 recovery, reconnect eligibility, broadcast logout between tabs; close UI on failed refresh | Frontend responsibility |
| BG-004 | Backend was not listening on localhost:5080 at first inspection | Live auth/chat/SQL journeys unavailable at that point | Probe health, record runtime verification honestly; do not read dev-mail/private uploads to obtain credentials | NEEDS_VERIFICATION |
| BG-005 | DevMailSink is local development delivery; no frontend route exposes mail tokens | Register/verification cannot be auto-completed without authorized token input | Manual test operator uses mail locally; frontend must never serve private-uploads | Later Auth slice |
| BG-006 | Several services conceal inaccessible/invalid action as 404 | UI cannot distinguish not-found from forbidden or invalid lifecycle | Say unavailable/access changed, refetch authorization; never claim a precise forbidden reason from 404 | Contract accepted |
| BG-007 | Single-instance session registry and presence | Multi-instance deployment needs distributed invalidation/presence | Portfolio local scope; document deployment limitation before scaling | No backend change authorized |

Backend change requests: **none submitted**. If a necessary gap is found, document its exact source, severity and concrete patch proposal here, then obtain approval before editing backend.

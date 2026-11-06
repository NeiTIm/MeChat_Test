# User flows

| Flow | Journey | Failure / permission behavior |
| --- | --- | --- |
| Authentication | Credentials → login → in-memory access → protected inbox; reload → CSRF cookie refresh → profile | Field validation; generic invalid state; offline/429/server error; failed refresh returns login |
| Account lifecycle | Register → manual verification token → login; reset via token; change/deactivate/session management | Token expiry, cooldown/conflict, revoked session; never reveal account existence |
| Users/friends | Search → public profile → request → incoming accept/reject or outgoing cancel | Blocked/ineligible targets unavailable; reverse request may accept |
| Private chat | Select conversation → resolve profile → cursor history → compose → server-confirmed message | Empty, pending access, unavailable, retry; stable clientMessageId on retry |
| Message requests | Stranger send → receiver inbox → accept/reject/block → main inbox | Directional rejection and explicit renewal respected |
| Message actions | Select visible message → reply/edit/recall/forward/react/context/history | Edit window; recalled/deleted content; destination visibility; confirmation where destructive |
| Group chat | Create → invite → accept → join-sequence history → send | Membership changed; disabled/dissolved; no historical visibility assumptions |
| Group administration | Details → permitted role action → confirm → refetch membership | Owner/Admin/Member distinctions; ownership transfer before leaving |
| Files | Validate → upload/progress → attach pending IDs → send → authorized blob preview/download | Invalid content/size, unauthorized read; object URL cleanup |
| Notifications | Server list/realtime invalidation → mark read → resolve reference → authorized destination | Reference may be inaccessible; notification is not authorization |
| Reports | Accessible target → reason → report → own report status | Server captures evidence; no private evidence API exposure |
| Appeals | Suspended user → appeal auth → isolated token → submit/read own appeal | Never enters normal chat or sends appeal token to hub |
| Admin | DB-confirmed Admin → claim report → evidence → warn/suspend/resolve; review appeal | Service permission/assignment, confirmations and reason; audit reflects actual server actions |
| Retention | Preview → paged plan → reasoned hold update | No automatic execute/delete UI; service eligibility authoritative |

The gate samples cover credential validation and representative chat layout interactions; all later flows remain TODO until approval and real integration testing.

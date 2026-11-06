# Persistence and business-rule boundaries

Read-only source review; no SQL executed and no schema or migrations changed.

- `Infrastructure/Persistence/AppDbContext.cs` and feature configurations describe SQL Server entities; repositories own query/locking behavior. Frontend never depends on EF entity shape instead of the DTO catalogue.
- `Auth/Repositories/IAuthRepository` exposes atomic token consumption, refresh revocation and session revocation. `AuthService` owns password hashing, lockout, token rotation and account lifecycle; client uses one refresh request and never attempts local token generation or role assignment.
- `Conversations/Repositories/PrivateChatRepository` filters hidden-through sequences, block/access state and per-user deletions; `PrivateChatService` owns directional access and send transactions. `lastSequence` is a cursor, **not a message count**.
- `Groups/Repositories/GroupRepository` applies each membership's join/leave sequence intervals, active status, per-user hiding and message deletions. History is descending by sequence with take+1 pagination. Rejoining does not grant all prior messages.
- `Messages/Repositories/MessageActionRepository` exposes visible context/history, hidden sequences and attachment projections. Service rechecks authorization around mutations and emits realtime events after commit; client HTTP cache must reauthorize rather than patch hidden content from an event.
- Friend/block transactions can revoke private access; group role/removal/transfer transactions alter send/read eligibility. UI visibility is a convenience; backend transaction checks decide whether an operation succeeds.
- Files use private local storage keyed independently of original filenames, content signature validation, and server-bound pending uploads. Read controllers authenticate every preview/download. Upload IDs must not become public static URLs.
- Report evidence/audit retention includes legal hold and independently approved purge/deletion switches. Admin preview endpoints do not authorize frontend database cleanup. This implementation does not expose or run purge operations.

SQL data integrity, race behavior, historical visibility and two-user transactional journeys remain NEEDS_VERIFICATION in frontend integration; backend tests were not rerun as a substitute for those journeys.

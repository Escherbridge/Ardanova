# Actor assertion replay ledger

The database-backed ledger is the cross-instance single-use boundary for BFF actor assertions. Request handling only claims the nonce; replay cleanup is isolated in a hosted maintenance service so database housekeeping cannot add authentication latency or change an accepted claim.

The worker runs immediately at startup and then once per minute. Each cycle uses a five-second budget and 500-row batches. PostgreSQL `FOR UPDATE SKIP LOCKED` lets Railway replicas claim disjoint expired rows without a global lock; each delete remains one atomic statement. The shared retention policy deletes only signed expiries more than five minutes old, which deliberately exceeds the API middleware's accepted clock skew. The Prisma-owned composite `(expiresAt, jti)` index keeps candidate selection bounded and ordered.

Cleanup failure is fail-open only for maintenance: the worker logs it and retries on the next interval, while assertion validation and replay rejection remain fail-closed. Keep the time budget, batch bound, retention cutoff, and replica-safe row claiming together when changing this workflow.

# SDK API security contract

## Session exchange

The web frontend must not invent SDK bearer tokens or treat an email address as an authorization code. `/api/sdk/auth/session` fails closed until the backend exposes a real exchange contract with a cryptographically random, single-use, short-lived code bound to the requesting game/client and an explicit user grant.

A future implementation must validate and consume that code server-side, issue a scoped expiring credential, avoid returning unnecessary profile fields, apply rate limits, set `Cache-Control: no-store`, and record revocation/audit evidence. Do not enable the route behind a client-controlled flag or restore predictable `sdk_session_<userId>` values.

## XP awards

`/api/sdk/actions` must not translate client-authored action names, task identifiers, or metadata into XP. It deliberately fails closed until the platform can derive the actor, event type, award amount, and source identifier from a verified backend event and atomically consume an idempotency key. A future implementation also needs resource ownership checks and abuse limits; the browser or game client must never call the generic XP award API through the BFF.

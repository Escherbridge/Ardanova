# Realtime trust boundary

The Next.js server is the only supported SignalR client. It authenticates the
service with a 32-byte-minimum `X-Api-Key` header. SignalR's
`accessTokenFactory` creates a fresh actor-assertion v2 envelope for every
negotiate, connect, and reconnect attempt. The transport presents that value as
an `Authorization: Bearer` credential when headers are available or as the
framework-standard `access_token` query value for WebSockets.

The assertion is signed for the logical `GET /hubs/ardanova` connection intent
with an empty content type, body, and idempotency key. SignalR-generated query
state is deliberately outside that logical target. The hub still validates the
same issuer, audience, subject, role, method, target, digest, nonce, lifetime,
and HMAC rules as HTTP actor assertions, then consumes the nonce in the shared
database replay ledger before binding the actor. Infrastructure logs must redact
`access_token`; application logs never include it.

The hub copies the validated actor identifier into the connection context and
uses that bound value for every later group decision. Never accept a user or
actor identifier as a hub-method argument for authorization.

Personal, project, guild, and conversation groups can carry non-public data.
Joining one requires the corresponding ownership or membership lookup. Project
creators count as owners even when no redundant `ProjectMember` row exists.
Conversation typing and read signals require the same membership validation as
conversation subscription; a prior REST fetch is not authorization for a later
hub invocation.

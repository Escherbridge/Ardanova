# Realtime server boundary

The browser reaches realtime data through the authenticated `/api/realtime` BFF route. Only the Next.js server connects to the .NET SignalR hub, and the shared API key must never enter browser code.

Every negotiate, connect, or reconnect attempt obtains a fresh canonical v2 actor assertion from `accessTokenFactory`. SignalR carries that short-lived token as its bearer/access token while `X-Api-Key` remains a server-only header. Never restore a plain actor-ID header or query parameter: the hub identity must be signed, audience-bound, time-bounded, and consumed once by the replay ledger.

The hub assertion signs `GET /hubs/ardanova` with an empty body and content type. SignalR-generated transport query parameters are deliberately outside that canonical target because the BFF does not know them before negotiation; the backend validator must reject any other path or method.

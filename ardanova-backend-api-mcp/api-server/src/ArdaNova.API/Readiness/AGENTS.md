# Hosted readiness

`ReleaseSchemaReadiness` is a deliberately narrow catalog gate for database
objects required immediately by the deployed API. It complements, rather than
replaces, the broader commerce migration preflight and release evidence.

Add an object here only when the running service cannot safely operate without
it. Keep checks read-only, bounded by the `/ready` request cancellation token,
and avoid returning catalog detail to public callers.

## Disclosure

`/ready` is anonymous because orchestrators and the frontend's `/api/ready`
probe call it without credentials, and both read only the status code. The
per-check body (economy legs, AZOA transport, custody reason, Algorand
provider) is a map of the deployment. `ReadinessDisclosure.ShowsDetails`
returns it only to a caller presenting the service key in `X-Api-Key`.
Everyone else gets `{ status, timestamp, service }` with the same 200/503.
Operators who need the breakdown:

    curl -H "X-Api-Key: $API_KEY" https://<api>/ready

## Anonymous probe cache

Every `/ready` call opens a database connection and runs the release-schema
catalog check. Loop 4 QA noted that anonymous callers are not rate-limited
(they have no actor to partition on), so anyone could turn `/ready` into one
database round-trip per request. `ReadinessSummaryCache` keeps the last verdict
for `Lifetime` (5 s). Within that window an anonymous probe gets the cached
200/503 summary without touching the database. Callers holding the service
key always get a fresh, detailed check, and every fresh check refreshes the
cache. A not-ready verdict is cached the same way, so a probe can lag reality
by up to 5 s in either direction. That is well inside any orchestrator's probe
interval.

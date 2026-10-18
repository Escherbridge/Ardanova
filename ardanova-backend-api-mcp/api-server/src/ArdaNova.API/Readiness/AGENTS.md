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

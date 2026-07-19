# Hosted readiness

`ReleaseSchemaReadiness` is a deliberately narrow catalog gate for database
objects required immediately by the deployed API. It complements, rather than
replaces, the broader commerce migration preflight and release evidence.

Add an object here only when the running service cannot safely operate without
it. Keep checks read-only, bounded by the `/ready` request cancellation token,
and avoid returning catalog detail to public callers.

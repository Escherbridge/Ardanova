# Policy test guidance

## Composition roots

The Application test process creates many intentionally independent PostgreSQL
data sources for scratch databases. Their distinct EF internal providers share
a process-wide cache, so its provider-count heuristic can fail a later
composition-root test based on execution order.

After `AddInfrastructure`, use the existing test-only
`SuppressManyServiceProvidersCreatedWarning` helper when resolving that root's
DbContext. It wraps the registered options factory and isolates only this
heuristic; the real Npgsql provider and native enum mappings remain intact.
Production registration and warning policy stay unchanged.

Keep `ValidateScopes`, actual service resolution and timer-registration
assertions. The feed tests must still reject missing dependencies or a DI cycle
and prove that the timer runs only when explicitly enabled; a fake DbContext or
weaker assertion would lose that coverage.

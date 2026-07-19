# Application dependency notes

## Object mapping

AutoMapper is pinned to 14.x, the final MIT-licensed line that includes the
Microsoft dependency-injection extensions and targets .NET 8. The application
targets .NET 10 and consumes that compatible library surface without a runtime
license key. Treat a major upgrade as an explicit API and licensing review.

AutoMapper 14 is covered by
[`GHSA-rvv3-g6hj-g44x`](https://github.com/advisories/GHSA-rvv3-g6hj-g44x),
an uncontrolled-recursion denial-of-service advisory. The patched upstream
lines use a different commercial/dual-license contract, so this release keeps
the MIT line and applies the advisory's explicit mitigation instead:
`MappingSecurityPolicy` forces `MaxDepth=64` over every registered type map.
`AddApplication` installs that global rule after profile discovery, and the
mapping-policy regression tests prove both full coverage and that a profile's
attempted unbounded depth is overwritten. Do not construct an application
mapper without applying the policy.

`api-server/Directory.Build.props` suppresses only that exact advisory URL
after the runtime mitigation. Never replace it with a warning-code, severity,
package-wide, or audit-wide suppression. Removing the policy requires either a
non-vulnerable dependency under acceptable license terms or removing
AutoMapper entirely.

## Generated secrets

`GeneratedSecretValidator` is the shared production boundary for service and
Azoa credentials. It enforces a minimum UTF-8 byte length and rejects
placeholder markers plus published configuration examples. New credentialed
integrations should reuse it instead of implementing length-only validation.

## AZOA workflow statuses

`AzoaRunState` mirrors every AZOA SDK `0.1.0` run status and adds only `Unknown`
for forward compatibility. Keep settlement reconciliation in the economic
outbox domain; it is not a workflow run status. Unknown node values retain
`RawStatus` so contract drift is visible instead of silently remapped.

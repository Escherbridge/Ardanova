# Application dependency notes

## Tier boundary

This project owns business logic and authorization. It never calls Azoa or Algorand directly:
any value-bearing workflow (custody, minting, allocation, exchange, cash-out, quests) is
delegated through the Infrastructure-owned ports (`ArdaNova.Infrastructure/Azoa/`,
`ArdaNova.Infrastructure/Algorand/`) that this layer depends on as interfaces. Only
`ArdaNova.Infrastructure` holds Azoa credentials and transport details; see
`ArdaNova.Infrastructure/Azoa/AGENTS.md` for the client/key boundaries this layer relies on.

## Object mapping

Object mapping uses Mapster 10 (MIT), not AutoMapper. AutoMapper 14.0.0 is
covered by high-severity uncontrolled-recursion advisories
([`GHSA-rvv3-g6hj-g44x`](https://github.com/advisories/GHSA-rvv3-g6hj-g44x),
[`GHSA-6vhm-j9h7-f9gh`](https://github.com/advisories/GHSA-6vhm-j9h7-f9gh),
[`GHSA-gpwp-5797-mjff`](https://github.com/advisories/GHSA-gpwp-5797-mjff)).
The upstream patches are on the 15.1 and 16.3 lines, which require a commercial
license key for commercial use, and `MaxDepth` does not close the later two
advisories. Do not bring AutoMapper back, and do not suppress these advisories.

`MappingProfile` registers the maps on a Mapster `TypeAdapterConfig` with
case-insensitive name matching. `MappingSecurityPolicy` still forces
`MaxDepth=64` on the default settings and on every registered rule, because
entity graphs contain cycles and Mapster will recurse until the stack dies
unless a depth is set. `AddApplication` registers that config and its mapper.
Do not construct an application mapper without applying the policy. The
mapping-policy tests prove every registered rule is bounded, that a later
unbounded depth is overwritten, and that a cyclic graph stops inside the limit.

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

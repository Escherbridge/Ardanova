# Economy mode reporting

`EconomyModeHealthCheck` is the only file here, and it is deliberately thin: it is the
API host's PROJECTION of the economy mode onto `/health`, `/ready`, and the startup log
banner — it does not decide the mode itself.

## Why the logic is not in this file

The actual resolution (`EconomyModeResolution.Resolve` / `.ResolveFromConfiguration`)
lives in `ArdaNova.Infrastructure`, not here, because the MCP host boots the same AZOA
settings through `AddInfrastructure` and must report the same words from the same code.
Duplicating the mapping per host is exactly how a person watching one host's banner and
another host's `/ready` response could end up being told two different economy modes
from what is actually one deployment's configuration. `Resolve` reads the container's
registered capabilities (`AlgorandProviderCapabilities`), so it reflects what was
actually wired at startup, not just what a config file claims.

## The two endpoints answer different questions

- **`/health`** (unauthenticated, Program.cs) is a coarse liveness probe. It carries
  only the `economy` tag (`"live"` / `"simulated"` / `"disabled"`) alongside the plain
  fact that the process is up — a healthy API process is NOT evidence of a working
  economy, and callers must not read it that way.
- **`/ready`** is the rich per-leg projection (`Describe`): funding checkout, the
  settlement outbox, custodial accounts, add-funds, and whether node- and chain-side
  value moves are real, each read from the services actually registered
  (`IFundingSettlementReadiness`, `IAzoaCustodialAccountGateway`,
  `ISettlementOutboxRuntimeCapability`, `AlgorandProviderCapabilities`) rather than
  echoed from configuration. `partiallyReal` and `liveWithNoWayIn` exist because a
  deployment can have a live chain leg with no way for a person to reach it, or the
  reverse, and either shape is a distinct failure a flat boolean would hide.

This directory answers "is the ECONOMY real and reachable"; `../Readiness/`
(`ReleaseSchemaReadiness`) answers a narrower question — whether the database objects
the API needs right now exist — and the two are composed into the same `/ready` response
rather than merged into one check, because a schema being present says nothing about
whether AZOA is configured, and a live economy says nothing about whether the schema was
migrated.

## Fail-closed at startup, not per-request

`EconomyModeResolution.ThrowIfNotLiveInProduction(economyMode, isProduction)` runs once
at boot (Program.cs), immediately after `Resolve`. A Production deployment that is not
actually Live refuses to START rather than serve traffic under a `/health` response that
would otherwise report it as merely "simulated" — the refusal happens before the first
request can be misled by it.

## The startup banner

`LogStartupBanner` writes the resolved mode at a level that matches how alarming it is: a
non-Live economy is never an Information line — Production logs it Critical, every other
host logs it Warning — so a simulated or disabled economy cannot scroll past a deploy log
unnoticed the way an Information line would.

---
type: plan
---
# Plan: backend loop-4 residuals

## Phase 1: Correctness (events lane)
- [ ] Atomic capacity check and counter in `EventService.RegisterAsync`/`UnregisterAsync` (item 1), with a concurrent flow test in `ArdaNova.Application.Tests`
- [ ] Viewer-aware event list queries (item 2); afterwards drop the total adjustment in `EventVisibilityFilter` or keep it as defence in depth

## Phase 2: Edge and keys (backend + client)
- [ ] IP-keyed anonymous limit in Next.js middleware or the proxy (item 3)
- [ ] Threat-model decision, then a read-only key for public procedures and actor-bound admin surfaces (item 4)
- [ ] Nonce-based CSP (item 5)

## Phase 3: Boundary completeness
- [ ] Validators for the remaining body DTOs, plus a reflection test that fails on any uncovered URL/email DTO (item 6)
- [ ] A shared nested-user projection used by every `*UserDto` (item 7)

## Phase 4: Tooling
- [ ] QA harness reads secrets from the environment (item 8)
- [ ] Make the C# generator safe to run in CI (item 9)

Each phase ends with an adversarial review (`/code-review high`, plus `/security-review` for phases 1-3) and a live QA re-run of the matching probe scripts.

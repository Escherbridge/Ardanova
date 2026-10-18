---
type: spec
title: Retire ardanova-ai-client
status: proposed
tags: [nova, ai, cleanup, security]
---
# Retire ardanova-ai-client (proposal)

## Why

`ardanova-ai-client/` is a FastAPI + CrewAI/LangGraph service (`/api/agent/analyze-project`,
`/api/agent/pitch/*`). Nothing in ArdaNova calls it, its workflow nodes return placeholders, and its
MCP client is a stub. Nova AI is now powered by the OVERSEASGENAI partner API through the .NET
backend (`conductor/tracks/nova-ai-ogai-partner`), which covers its main intent (project viability
analysis) with a real, tested, evidence-citing pipeline. Keeping a second, dormant AI path means a
second provider bill, a second prompt surface to keep brand- and safety-clean, and a second attack
surface: until loop 1 its LLM-spending routes had no authentication at all.

## Interim hardening (loop 1)

Every `/api/agent/*` route now requires `X-Api-Key` equal to `INBOUND_API_KEY` (constant-time
compare) and answers 503 when that variable is unset, so a stray deployment cannot spend the
provider budget. Not runtime-tested here: FastAPI is not installed in this environment.

## Proposal

1. Confirm no deployment runs it (Railway service list) and no caller exists (`git grep ardanova-ai-client`).
2. Port anything worth keeping: the pitch/deck idea maps to OGAI's later waves, not to this service.
3. Delete `ardanova-ai-client/`, its Dockerfile and any CI or Railway entry; update `README`/architecture docs.

## Acceptance

- No service, CI job or doc references `ardanova-ai-client`; Nova AI flows are unaffected.

## Not decided here

Deletion itself: this track only proposes it. The owner confirms step 1 before step 3.

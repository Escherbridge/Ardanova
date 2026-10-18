# ardanova-ai-client — why

Nova AI's orchestration tier. It reasons (CrewAI/LangGraph, Claude-backed); it never holds
platform state or financial credentials itself.

## Boundary rules

- **Data and actions reach this service only through .NET MCP tools** (`src/mcp/client.py` →
  `ArdaNova.MCP/Tools/`). It inherits whatever authorization limits those tools carry — it has no
  separate authority, no DB connection, and no way to bypass the HTTP API's checks.
- **No Azoa credentials, ever.** Only `ArdaNova.Infrastructure/Azoa/` in the .NET tier holds those.
  If a workflow here ever needs a financial-workflow result, it must ask for it through an MCP
  tool that the backend has decided is safe to expose — never a direct call.
- **The MCP stdio host has no trusted end-user principal** (see
  `ardanova-backend-api-mcp/api-server/src/ArdaNova.MCP/Tools/AGENTS.md`), so MCP tools are
  discovery-only. Value-, role-, credential-, and governance-mutating actions are out of scope for
  anything this service can call — not a missing feature, a deliberate boundary.
- `MCP_TOOL_REGISTRY` in `src/core/tools/mcp_tool.py` is **stale against that boundary**: its
  `exchange`, `escrow`, and `governance` (delegation) domains list write tools
  (`escrow_create`, `escrow_release`, `delegation_create`, `swap_create`, …) that have no
  corresponding `*Tools.cs` file in `ArdaNova.MCP/Tools/` — those were intentionally removed
  server-side for lacking a verified principal. Treat those registry entries as aspirational, not
  current capability, until the backend reintroduces them with real authorization.

## CrewAI vs LangGraph split

- **CrewAI** (`src/crews/`, built via `core/agent_builder.py` + `core/crew_builder.py`) answers one
  bounded question with a small team of role-played agents running sequential tasks — "what's a
  good title/problem/solution for this idea," "what's a realistic funding goal."
- **LangGraph** (`src/workflows/`) is the layer above that decides *which* crew(s) run, in what
  order, and on what condition (deck-or-not, simple-vs-complex analysis). A crew never calls
  another crew directly; routing and branching live exclusively in the graph.

## Known gaps

- `MCPClient.call_tool` / `list_tools` / `get_resource` (`src/mcp/client.py`) are unimplemented
  stubs — nothing in this service can reach the MCP server today, regardless of what the crews do.
- The workflow nodes that should kick off crews (`run_concept_crew`, `run_strategy_crew`,
  `run_positioning_crew`, `run_full_analysis`, `run_quick_analysis`) return hardcoded placeholder
  data instead of invoking the crews in `src/crews/` — the crews are built but never called.
- `src/agents/` (`BaseAgent`, `ProjectAnalystAgent`) is a second, parallel agent abstraction that
  predates the CrewAI/LangGraph split and isn't wired into `main.py`/`routes.py` at all. Don't
  extend it; new work goes through `core/` + `crews/` + `workflows/`.
- **Not wired into the product**: grep of `ardanova-client/src` finds no reference to this
  service, and the `ai-orchestrator` block in the root `docker-compose.yml` is fully commented
  out. Nova AI is implemented but dormant until the client is pointed at it.
- Dockerfile sets `HOME=/app` + `CREWAI_STORAGE_DIR=/app/.crewai_storage` (commit `cfee56c`)
  because CrewAI resolves its storage path under `$HOME` at import time and the non-root
  `appuser` has no `$HOME` by default — drop this and the container fails on crew init. Keep it
  if `appuser`'s UID/GID or base image ever changes.

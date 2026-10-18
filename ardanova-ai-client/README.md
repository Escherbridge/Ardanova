# ArdaNova AI Client

Python FastAPI service that operates **Nova AI**, ArdaNova's platform assistant. It is the
fourth tier in ArdaNova's architecture: it orchestrates Claude-backed agents (via CrewAI) and
LangGraph workflows to turn a prompt into structured project data (pitches, viability analysis),
optionally generating a pitch deck through Gamma.

## Where it sits

```
Browser → ardanova-client (Next.js, UX) → tRPC → ardanova-backend-api-mcp (.NET, BFF)
                                                        ↑
                                      ardanova-ai-client (this service) ── MCP tool calls
```

- This service talks to the **.NET MCP server** only (`src/mcp/client.py`, default
  `http://localhost:8080`). It never touches PostgreSQL, Azoa, or Algorand directly — there is no
  database client, Azoa SDK, or chain client anywhere in `src/`.
- It is not currently called by `ardanova-client`: nothing in `ardanova-client/src` references
  this service, and the `ai-orchestrator` block in the repo-root `docker-compose.yml` is entirely
  commented out. Treat Nova AI as **built but not yet wired into the product** — see
  [AGENTS.md](./AGENTS.md) for the rest of the current gaps.
- CORS is pre-configured for `http://localhost:3000` / `https://localhost:3000` (the Next.js
  client's origins) in anticipation of that integration.

## Directory layout

```
ardanova-ai-client/
├── Dockerfile
├── requirements.txt
├── .env.example
└── src/
    ├── api/
    │   ├── main.py              # FastAPI app, lifespan (MCPClient/AgentBuilder/CrewBuilder), /health
    │   └── routes.py            # /api/agent/* endpoints, delegate straight to workflows/
    ├── config/
    │   └── settings.py          # Settings (pydantic-settings, env-driven)
    ├── core/
    │   ├── agent_builder.py     # AgentBuilder: CrewAI Agent factory (LLM + MCP tool injection)
    │   ├── crew_builder.py      # CrewBuilder: fluent Agent/Task → Crew composition
    │   ├── graph_builder.py     # GraphBuilder: fluent LangGraph StateGraph composition
    │   ├── state.py             # TypedDict state schemas, one per workflow
    │   └── tools/
    │       ├── mcp_tool.py      # MCPTool + MCP_TOOL_REGISTRY: wraps MCP backend calls as CrewAI tools
    │       └── gamma_tool.py    # GammaGenerateTool / GammaStatusTool (Gamma API)
    ├── crews/
    │   ├── pitch_crew.py            # concept / strategy / positioning crews
    │   └── project_analysis_crew.py # viability / market / financial-projection crew
    ├── workflows/
    │   ├── pitch_generation.py  # LangGraph: parse_prompt → concept → strategy → positioning
    │   │                         #   → assemble → (generate_deck | skip_deck) → finalize
    │   └── project_analysis.py  # LangGraph: classify → (full_analysis | quick_analysis) → finalize
    ├── agents/
    │   ├── base.py              # BaseAgent ABC (process/stream) — a separate, unused agent model
    │   └── project_analyst.py   # ProjectAnalystAgent — not wired into any route (see AGENTS.md)
    ├── mcp/
    │   └── client.py            # MCPClient: httpx client to the .NET MCP server
    └── orchestrator/            # placeholder package; currently empty
```

There is no `tests/` directory in this service today.

## HTTP API

All routes except `/health` are mounted under `/api/agent` (`src/api/routes.py`).

| Endpoint                   | Method | Description |
| --------------------------- | ------ | ------------ |
| `/health`                   | GET    | Liveness check (`src/api/main.py`) |
| `/api/agent/analyze-project` | POST  | Run the project-analysis LangGraph workflow (viability, success/impact/profitability scores, recommended funding goal) |
| `/api/agent/pitch/generate`  | POST  | Full prompt-to-project pipeline: concept → strategy → positioning → assembly → optional Gamma deck |
| `/api/agent/pitch/enhance`   | POST  | Same pipeline, but seeded with `partial_data` so already-filled fields are skipped |
| `/api/agent/pitch/deck`      | POST  | Generate only a Gamma pitch deck from project data that already exists |

## Crews & workflows

- **CrewAI crews** (`src/crews/`) model one bounded reasoning job as a small team of role-played
  agents with sequential tasks: `pitch_crew.py` has a concept crew (title/problem/solution +
  categories), a strategy crew (milestones/resources/timeline), and a positioning crew
  (tags/audience/impact/description); `project_analysis_crew.py` has a viability analyst, market
  researcher, and financial projector.
- **LangGraph workflows** (`src/workflows/`) are the routing layer on top: one `StateGraph` per
  endpoint that decides which crew(s) run, in what order, and handles conditional branches
  (deck-or-not, complex-vs-simple analysis).
- **Current implementation state:** the crews above are fully built, but the workflow node
  functions that should call them (`run_concept_crew`, `run_strategy_crew`,
  `run_positioning_crew`, `run_full_analysis`, `run_quick_analysis`) are stubs that return
  hardcoded placeholder data — see the `TODO` comments in `src/workflows/`. `src/mcp/client.py`'s
  `call_tool`/`list_tools`/`get_resource` are likewise unimplemented, so no crew can reach the MCP
  server yet even once wired up.

## Configuration

Settings load from environment variables (`src/config/settings.py`, `pydantic-settings`,
optional `.env` file). No secret values are shown below — set real values via your own `.env` or
deployment environment.

| Variable | Default | Notes |
| --- | --- | --- |
| `ANTHROPIC_API_KEY` | `""` | Claude API key |
| `MCP_SERVER_URL` | `http://localhost:8080` | .NET MCP server base URL |
| `API_KEY` | `""` | Sent as `X-Api-Key` to the MCP server |
| `PORT` | `8081` | |
| `DEBUG` | `false` | |
| `LLM_PROVIDER` | `anthropic` | `anthropic` or `openai`; anything else is passed through as-is |
| `LLM_MODEL` | `claude-sonnet-4-20250514` | |
| `LLM_TEMPERATURE` | `0.7` | |
| `CREW_VERBOSE` | `true` | CrewAI agent/crew verbosity |
| `CREW_MAX_ITERATIONS` | `20` | Default `max_iter` for CrewAI agents |
| `GAMMA_API_KEY` | `""` | Required for `pitch/deck`; tools return a configuration message if unset |
| `GAMMA_BASE_URL` | `https://public-api.gamma.app/v1.0` | |

## Running locally

```bash
# From ardanova-ai-client/
python -m venv venv
source venv/bin/activate   # venv\Scripts\activate on Windows

pip install -r requirements.txt

uvicorn src.api.main:app --reload --port 8081
```

## Running with Docker

```bash
# From ardanova-ai-client/
docker build -t ardanova-ai-client .
docker run -p 8081:8081 --env-file .env ardanova-ai-client
```

The image runs as a non-root `appuser` (uid/gid 1001). CrewAI resolves its storage directory
under `$HOME` at import time, and a `--system` user has no writable `$HOME` by default, so the
Dockerfile sets `HOME=/app` and `CREWAI_STORAGE_DIR=/app/.crewai_storage` (created and
`chown`'d before the `USER appuser` switch) — without this the container fails on crew
initialization.

A container health check polls `GET /health` every 30s.

The `ai-orchestrator` service block in the repo-root `docker-compose.yml` is currently commented
out; uncomment it to run this service alongside the rest of the stack.

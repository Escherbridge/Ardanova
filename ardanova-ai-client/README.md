# ArdaNova AI Client

Python AI Orchestrator Service for the ArdaNova platform.

## Overview

This service provides AI agent orchestration, connecting to the .NET MCP server for tool execution.

## Architecture

```
ardanova-ai-client/
├── src/
│   ├── api/                    # FastAPI endpoints
│   │   ├── __init__.py
│   │   ├── main.py             # FastAPI app
│   │   └── routes/             # API routes
│   │       ├── __init__.py
│   │       ├── agent.py        # Agent chat endpoints
│   │       └── health.py       # Health check
│   │
│   ├── orchestrator/           # AI agent orchestration
│   │   ├── __init__.py
│   │   ├── router.py           # Agent routing & selection
│   │   ├── context.py          # Context aggregation
│   │   └── streaming.py        # Response streaming
│   │
│   ├── agents/                 # Agent definitions
│   │   ├── __init__.py
│   │   ├── base.py             # Base agent class
│   │   ├── project_manager.py  # Sprint planning, task breakdown
│   │   ├── pitch_generator.py  # Gamma API integration
│   │   ├── governance.py       # DAO proposals, voting guidance
│   │   ├── token_analyst.py    # Tokenomics, ICO analysis
│   │   └── code_assistant.py   # Technical guidance
│   │
│   ├── mcp/                    # MCP client
│   │   ├── __init__.py
│   │   ├── client.py           # MCP protocol client
│   │   └── tools.py            # Tool execution
│   │
│   └── config/                 # Configuration
│       ├── __init__.py
│       └── settings.py         # App settings
│
├── tests/                      # Test suite
│   ├── __init__.py
│   ├── test_agents.py
│   └── test_mcp.py
│
├── requirements.txt            # Python dependencies
├── Dockerfile                  # Container deployment
└── README.md                   # This file
```

## Requirements

- Python 3.11+
- FastAPI
- anthropic (Claude API)
- httpx (MCP client)

## Development

```bash
# Create virtual environment
python -m venv venv
source venv/bin/activate  # or `venv\Scripts\activate` on Windows

# Install dependencies
pip install -r requirements.txt

# Run development server
uvicorn src.api.main:app --reload --port 8081
```

## Environment Variables

```env
# Claude API
ANTHROPIC_API_KEY=your-api-key

# MCP Server
MCP_SERVER_URL=http://localhost:8080

# API Key for backend
API_KEY=

# Server
PORT=8081
```

## API Endpoints

| Endpoint             | Method | Description             |
| -------------------- | ------ | ----------------------- |
| `/health`            | GET    | Health check            |
| `/api/agent/chat`    | POST   | Send message to agent   |
| `/api/agent/stream`  | POST   | Streaming chat response |
| `/api/agent/context` | GET    | Get agent context       |

## Agent Commands

The AI agents support natural language commands:

- `/plan sprint` - Suggest items for next sprint
- `/breakdown [feature]` - Break down feature into tasks
- `/assign` - Suggest task assignments
- `/pitch [template]` - Generate pitch presentation
- `/propose [type]` - Draft governance proposal
- `/tokenomics` - Generate tokenomics report

## Integration

The AI client connects to:

1. **Claude API** - For LLM inference
2. **ArdaNova MCP Server** - For tool execution (40+ tools)
3. **Gamma API** - For pitch generation

## Deployment

```bash
# Build container
docker build -t ardanova-ai-client .

# Run container
docker run -p 8081:8081 --env-file .env ardanova-ai-client
```

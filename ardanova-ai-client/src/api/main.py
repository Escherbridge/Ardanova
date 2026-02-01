"""
ArdaNova AI Client - FastAPI Application

Main entry point for the AI orchestrator service.
Manages lifecycle of shared resources (MCP client, builders).
"""

from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from ..config.settings import settings
from ..core.agent_builder import AgentBuilder
from ..core.crew_builder import CrewBuilder
from ..mcp.client import MCPClient


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifecycle: initialize and clean up shared resources."""
    # Startup
    mcp_client = MCPClient(
        server_url=settings.mcp_server_url,
        api_key=settings.api_key,
    )
    agent_builder = AgentBuilder(settings, mcp_client)
    crew_builder = CrewBuilder(agent_builder)

    # Store on app state for access in routes if needed
    app.state.mcp_client = mcp_client
    app.state.agent_builder = agent_builder
    app.state.crew_builder = crew_builder

    yield

    # Shutdown
    await mcp_client.close()


app = FastAPI(
    title="ArdaNova AI Client",
    description="AI Orchestrator Service for the ArdaNova platform",
    version="0.2.0",
    lifespan=lifespan,
)

# CORS configuration
app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:3000",
        "https://localhost:3000",
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "ardanova-ai-client",
        "version": "0.2.0",
    }


from .routes import router as agent_router

app.include_router(agent_router)

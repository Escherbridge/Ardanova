"""
ArdaNova AI Client - FastAPI Application

Main entry point for the AI orchestrator service.
"""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI(
    title="ArdaNova AI Client",
    description="AI Orchestrator Service for the ArdaNova platform",
    version="0.1.0",
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
    }


# TODO: Import and include routers
# from .routes import agent, health
# app.include_router(agent.router, prefix="/api/agent", tags=["agent"])

"""
Application Settings

Configuration for the AI orchestrator service.
"""

from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Application settings loaded from environment variables"""

    # Claude API
    anthropic_api_key: str = ""

    # MCP Server
    mcp_server_url: str = "http://localhost:8080"

    # API Key for backend
    api_key: str = ""

    # Server
    port: int = 8081
    debug: bool = False

    # LLM Configuration
    llm_provider: str = "anthropic"
    llm_model: str = "claude-sonnet-4-20250514"
    llm_temperature: float = 0.7

    # CrewAI Defaults
    crew_verbose: bool = True
    crew_max_iterations: int = 20

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"


settings = Settings()

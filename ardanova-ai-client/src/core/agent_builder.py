"""
AgentBuilder

Factory for creating CrewAI agents with sensible defaults.
Manages LLM configuration and MCP tool injection so callers only
need to specify role, goal, and backstory.
"""

from __future__ import annotations

from typing import Any

from crewai import Agent

from ..config.settings import Settings
from ..mcp.client import MCPClient
from .tools.mcp_tool import MCPToolFactory


class AgentBuilder:
    """Factory for creating CrewAI Agent instances.

    Centralizes LLM configuration, default parameters, and tool injection.
    All agents built through this factory share the same LLM and MCP connection.

    Usage:
        builder = AgentBuilder(settings, mcp_client)

        analyst = builder.build(
            role="Viability Analyst",
            goal="Assess project viability",
            backstory="Expert in evaluating startup potential",
            tool_domains=["project", "user"],
        )
    """

    def __init__(self, settings: Settings, mcp_client: MCPClient):
        self._settings = settings
        self._mcp_client = mcp_client
        self._tool_factory = MCPToolFactory(mcp_client)
        self._llm = self._build_llm()

    def _build_llm(self) -> Any:
        """Build the LLM instance from settings.

        Returns a string model identifier for CrewAI, which handles
        provider routing internally. For Anthropic models, CrewAI
        uses the ANTHROPIC_API_KEY env var automatically.
        """
        provider = self._settings.llm_provider
        model = self._settings.llm_model

        if provider == "anthropic":
            return f"anthropic/{model}"
        elif provider == "openai":
            return f"openai/{model}"
        else:
            return model

    @property
    def tool_factory(self) -> MCPToolFactory:
        """Access the MCP tool factory for creating tools outside of agents."""
        return self._tool_factory

    def build(
        self,
        role: str,
        goal: str,
        backstory: str,
        *,
        tools: list[Any] | None = None,
        tool_domains: list[str] | None = None,
        verbose: bool | None = None,
        allow_delegation: bool = False,
        max_iter: int | None = None,
        **overrides: Any,
    ) -> Agent:
        """Create a CrewAI Agent with defaults from settings.

        Args:
            role: The agent's role/title (e.g., "Market Researcher").
            goal: What the agent is trying to achieve.
            backstory: Context that shapes the agent's behavior.
            tools: Explicit list of CrewAI tool instances.
            tool_domains: MCP domains to auto-include (e.g., ["project", "user"]).
                         Tools from these domains are generated via MCPToolFactory.
            verbose: Override crew_verbose from settings.
            allow_delegation: Whether this agent can delegate to others.
            max_iter: Override crew_max_iterations from settings.
            **overrides: Additional kwargs passed directly to Agent().

        Returns:
            A configured CrewAI Agent instance.
        """
        # Merge tools: explicit tools + domain-generated MCP tools
        all_tools = list(tools or [])
        if tool_domains:
            all_tools.extend(self._tool_factory.create_tools(*tool_domains))

        return Agent(
            role=role,
            goal=goal,
            backstory=backstory,
            llm=self._llm,
            tools=all_tools or None,
            verbose=verbose if verbose is not None else self._settings.crew_verbose,
            allow_delegation=allow_delegation,
            max_iter=max_iter or self._settings.crew_max_iterations,
            **overrides,
        )

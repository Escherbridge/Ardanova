"""
MCP Tool Wrapper

Wraps MCP backend tool calls as CrewAI-compatible BaseTool instances.
MCPToolFactory generates tools by domain so crews only get the tools they need.
"""

from __future__ import annotations

import asyncio
import json
from typing import Any, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from ...mcp.client import MCPClient


class MCPToolInput(BaseModel):
    """Generic input schema for MCP tools. Arguments are passed as a JSON string."""

    arguments: str = Field(
        default="{}",
        description="JSON string of arguments to pass to the MCP tool",
    )


class MCPTool(BaseTool):
    """CrewAI tool that delegates to an MCP backend tool.

    Each instance wraps a single MCP tool (e.g., project_get_by_id).
    The _run method calls the MCP client synchronously (via asyncio)
    so it works within CrewAI's execution model.
    """

    name: str
    description: str
    args_schema: Type[BaseModel] = MCPToolInput
    mcp_client: Any = Field(exclude=True)  # MCPClient, excluded from schema
    mcp_tool_name: str = ""

    model_config = {"arbitrary_types_allowed": True}

    def _run(self, arguments: str = "{}") -> str:
        """Execute the MCP tool synchronously."""
        try:
            parsed_args = json.loads(arguments)
        except json.JSONDecodeError:
            return f"Error: Invalid JSON arguments: {arguments}"

        try:
            loop = asyncio.get_event_loop()
            if loop.is_running():
                import concurrent.futures

                with concurrent.futures.ThreadPoolExecutor() as pool:
                    result = pool.submit(
                        asyncio.run,
                        self.mcp_client.call_tool(self.mcp_tool_name, parsed_args),
                    ).result()
            else:
                result = loop.run_until_complete(
                    self.mcp_client.call_tool(self.mcp_tool_name, parsed_args)
                )

            if result is None:
                return "Tool returned no result (MCP client not yet implemented)"

            return json.dumps(result) if not isinstance(result, str) else result
        except NotImplementedError:
            return f"MCP tool '{self.mcp_tool_name}' is not yet implemented"
        except Exception as e:
            return f"Error calling MCP tool '{self.mcp_tool_name}': {e}"


# Registry of known MCP tools organized by domain.
# Maps domain -> list of (tool_name, description) tuples.
MCP_TOOL_REGISTRY: dict[str, list[tuple[str, str]]] = {
    "project": [
        ("project_get_by_id", "Get a project by its ID"),
        ("project_get_by_slug", "Get a project by its URL slug"),
        ("project_get_all", "List all projects"),
        ("project_get_paged", "Get paginated list of projects"),
        ("project_get_by_user", "Get projects created by a specific user"),
        ("project_get_by_category", "Get projects filtered by category"),
        ("project_get_featured", "Get featured projects"),
    ],
    "user": [
        ("user_get_by_id", "Get a user by their ID"),
        ("user_get_by_email", "Get a user by their email address"),
        ("user_get_paged", "Get paginated list of users"),
    ],
    "guild": [
        ("guild_get_by_id", "Get a guild by its ID"),
        ("guild_get_by_slug", "Get a guild by its URL slug"),
        ("guild_get_paged", "Get paginated list of guilds"),
        ("guild_get_by_owner", "Get guilds owned by a specific user"),
        ("guild_get_verified", "Get verified guilds"),
    ],
    "activity": [
        ("activity_get_by_id", "Get an activity by its ID"),
        ("activity_get_by_user_id", "Get activities for a specific user"),
        ("activity_get_by_project_id", "Get activities for a specific project"),
        ("activity_create", "Record a new activity"),
    ],
    "notification": [
        ("notification_get_by_id", "Get a notification by its ID"),
        ("notification_get_by_user_id", "Get notifications for a user"),
        ("notification_get_unread", "Get unread notifications for a user"),
        ("notification_create", "Create a new notification"),
    ],
    "exchange": [
        ("swap_get_by_id", "Get a token swap by its ID"),
        ("swap_create", "Create a new token swap"),
        ("pool_get_all", "Get all liquidity pools"),
        ("pool_get_active", "Get active liquidity pools"),
    ],
    "escrow": [
        ("escrow_get_by_id", "Get a task escrow by its ID"),
        ("escrow_get_by_task_id", "Get escrows for a specific task"),
        ("escrow_create", "Create a new task escrow"),
        ("escrow_release", "Release funds from escrow"),
    ],
    "wallet": [
        ("wallet_get_by_id", "Get a wallet by its ID"),
        ("wallet_get_by_user_id", "Get wallets for a specific user"),
        ("wallet_get_primary", "Get a user's primary wallet"),
    ],
    "governance": [
        ("delegation_get_by_id", "Get a delegation by its ID"),
        ("delegation_get_active_by_project", "Get active delegations for a project"),
        ("delegation_get_total_power", "Get total voting power for a project"),
        ("delegation_create", "Create a new delegation"),
    ],
    "gamification": [
        ("streak_get_by_user_id", "Get a user's activity streak"),
        ("referral_get_by_id", "Get a referral by its ID"),
        ("referral_get_by_referrer", "Get referrals made by a user"),
    ],
}


class MCPToolFactory:
    """Factory that generates CrewAI tool instances from the MCP tool registry.

    Usage:
        factory = MCPToolFactory(mcp_client)
        project_tools = factory.create_tools("project")
        all_tools = factory.create_tools()  # all domains
    """

    def __init__(self, mcp_client: MCPClient):
        self._mcp_client = mcp_client

    def create_tools(self, *domains: str) -> list[MCPTool]:
        """Create CrewAI tool instances for the specified MCP domains.

        Args:
            *domains: Domain names to include (e.g., "project", "user").
                      If empty, creates tools for all registered domains.

        Returns:
            List of MCPTool instances ready for use with CrewAI agents.
        """
        target_domains = domains if domains else MCP_TOOL_REGISTRY.keys()
        tools: list[MCPTool] = []

        for domain in target_domains:
            entries = MCP_TOOL_REGISTRY.get(domain, [])
            for tool_name, description in entries:
                tools.append(
                    MCPTool(
                        name=tool_name,
                        description=description,
                        mcp_client=self._mcp_client,
                        mcp_tool_name=tool_name,
                    )
                )

        return tools

    def create_tool(self, tool_name: str, description: str | None = None) -> MCPTool:
        """Create a single MCPTool instance by name.

        Args:
            tool_name: The MCP tool name (e.g., "project_get_by_id")
            description: Optional override for the tool description.
                         If not provided, looks up from registry.
        """
        if description is None:
            for entries in MCP_TOOL_REGISTRY.values():
                for name, desc in entries:
                    if name == tool_name:
                        description = desc
                        break
                if description is not None:
                    break

        if description is None:
            description = f"MCP tool: {tool_name}"

        return MCPTool(
            name=tool_name,
            description=description,
            mcp_client=self._mcp_client,
            mcp_tool_name=tool_name,
        )

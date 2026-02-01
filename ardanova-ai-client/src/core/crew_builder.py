"""
CrewBuilder

Fluent factory for composing CrewAI crews.
Stacks on top of AgentBuilder - you add agents (via builder params)
and tasks, then build() returns a ready-to-kickoff Crew.
"""

from __future__ import annotations

from typing import Any, Type

from crewai import Agent, Crew, Process, Task
from pydantic import BaseModel

from .agent_builder import AgentBuilder


class CrewBuilder:
    """Fluent API for building CrewAI Crews.

    Stacks on AgentBuilder: agents are created through the builder's
    factory, ensuring consistent LLM/tool configuration.

    Usage:
        crew = (
            CrewBuilder(agent_builder)
            .add_agent(
                role="Viability Analyst",
                goal="Assess project viability",
                backstory="Expert in evaluating project potential",
                tool_domains=["project"],
            )
            .add_agent(
                role="Financial Projector",
                goal="Calculate funding requirements",
                backstory="Financial modeling expert",
            )
            .add_task(
                description="Evaluate the viability of this project: {project_data}",
                expected_output="Viability score (0-1) with rationale",
                agent_index=0,
            )
            .add_task(
                description="Based on viability analysis, project a funding goal",
                expected_output="Recommended funding goal in USD with breakdown",
                agent_index=1,
                context_tasks=[0],
            )
            .build()
        )

        result = crew.kickoff(inputs={"project_data": "..."})
    """

    def __init__(self, agent_builder: AgentBuilder):
        self._agent_builder = agent_builder
        self._agents: list[Agent] = []
        self._tasks: list[Task] = []

    def add_agent(
        self,
        role: str,
        goal: str,
        backstory: str,
        *,
        tools: list[Any] | None = None,
        tool_domains: list[str] | None = None,
        allow_delegation: bool = False,
        **kwargs: Any,
    ) -> CrewBuilder:
        """Add an agent to the crew via AgentBuilder.

        Args:
            role: The agent's role/title.
            goal: What the agent is trying to achieve.
            backstory: Context for agent behavior.
            tools: Explicit CrewAI tools for this agent.
            tool_domains: MCP domains to include.
            allow_delegation: Whether this agent can delegate.
            **kwargs: Additional AgentBuilder.build() kwargs.

        Returns:
            self for chaining.
        """
        agent = self._agent_builder.build(
            role=role,
            goal=goal,
            backstory=backstory,
            tools=tools,
            tool_domains=tool_domains,
            allow_delegation=allow_delegation,
            **kwargs,
        )
        self._agents.append(agent)
        return self

    def add_existing_agent(self, agent: Agent) -> CrewBuilder:
        """Add a pre-built Agent instance directly.

        Use this when you need to reuse an agent across multiple crews
        or when the agent was built outside of the CrewBuilder flow.
        """
        self._agents.append(agent)
        return self

    def add_task(
        self,
        description: str,
        expected_output: str,
        *,
        agent_index: int | None = None,
        agent: Agent | None = None,
        output_pydantic: Type[BaseModel] | None = None,
        context_tasks: list[int] | None = None,
        async_execution: bool = False,
        **kwargs: Any,
    ) -> CrewBuilder:
        """Add a task to the crew.

        Args:
            description: What the task entails. Supports {variable} interpolation.
            expected_output: Clear definition of the expected result.
            agent_index: Index into the agents list (added via add_agent).
                         Mutually exclusive with `agent`.
            agent: A specific Agent instance. Mutually exclusive with `agent_index`.
            output_pydantic: Pydantic model for structured output.
            context_tasks: List of task indices whose outputs feed into this task.
            async_execution: Whether to run this task asynchronously.
            **kwargs: Additional Task() kwargs.

        Returns:
            self for chaining.
        """
        # Resolve agent
        resolved_agent = agent
        if agent_index is not None:
            if agent_index < 0 or agent_index >= len(self._agents):
                raise IndexError(
                    f"agent_index {agent_index} out of range "
                    f"(have {len(self._agents)} agents)"
                )
            resolved_agent = self._agents[agent_index]

        # Resolve context tasks
        context = None
        if context_tasks:
            context = []
            for idx in context_tasks:
                if idx < 0 or idx >= len(self._tasks):
                    raise IndexError(
                        f"context_tasks index {idx} out of range "
                        f"(have {len(self._tasks)} tasks)"
                    )
                context.append(self._tasks[idx])

        task = Task(
            description=description,
            expected_output=expected_output,
            agent=resolved_agent,
            output_pydantic=output_pydantic,
            context=context,
            async_execution=async_execution,
            **kwargs,
        )
        self._tasks.append(task)
        return self

    def build(
        self,
        process: str = "sequential",
        **kwargs: Any,
    ) -> Crew:
        """Build the Crew from accumulated agents and tasks.

        Args:
            process: "sequential" or "hierarchical".
            **kwargs: Additional Crew() kwargs (e.g., manager_llm for hierarchical).

        Returns:
            A configured CrewAI Crew ready for kickoff().
        """
        if not self._agents:
            raise ValueError("Cannot build a crew with no agents")
        if not self._tasks:
            raise ValueError("Cannot build a crew with no tasks")

        process_enum = (
            Process.hierarchical if process == "hierarchical" else Process.sequential
        )

        crew = Crew(
            agents=list(self._agents),
            tasks=list(self._tasks),
            process=process_enum,
            verbose=self._agent_builder._settings.crew_verbose,
            **kwargs,
        )

        return crew

    def reset(self) -> CrewBuilder:
        """Clear agents and tasks for reuse.

        Returns:
            self for chaining.
        """
        self._agents.clear()
        self._tasks.clear()
        return self

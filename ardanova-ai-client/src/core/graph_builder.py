"""
GraphBuilder

Factory for building LangGraph StateGraph workflows.
Each API endpoint gets its own graph that routes to appropriate
CrewAI crews or direct function calls.
"""

from __future__ import annotations

import asyncio
from typing import Any, Callable, Type

from langgraph.graph import END, START, StateGraph

from .crew_builder import CrewBuilder


class GraphBuilder:
    """Fluent API for building LangGraph StateGraph workflows.

    Provides helpers for adding crew nodes (which build and execute
    CrewAI crews) and function nodes, with routing support.

    Usage:
        graph = (
            GraphBuilder(ProjectAnalysisState)
            .add_function_node("classify", classify_project)
            .add_crew_node("full_analysis", build_full_analysis_crew)
            .add_crew_node("quick_analysis", build_quick_analysis_crew)
            .add_function_node("finalize", finalize_result)
            .set_entry("classify")
            .add_router("classify", route_by_complexity, {
                "complex": "full_analysis",
                "simple": "quick_analysis",
            })
            .add_edge("full_analysis", "finalize")
            .add_edge("quick_analysis", "finalize")
            .add_terminal("finalize")
            .build()
        )

        result = await graph.ainvoke(initial_state)
    """

    def __init__(self, state_class: Type):
        self._state_class = state_class
        self._graph = StateGraph(state_class)
        self._crew_builder: CrewBuilder | None = None

    def set_crew_builder(self, crew_builder: CrewBuilder) -> GraphBuilder:
        """Set the CrewBuilder instance used by crew nodes.

        Args:
            crew_builder: The CrewBuilder to use for building crews in nodes.

        Returns:
            self for chaining.
        """
        self._crew_builder = crew_builder
        return self

    def add_function_node(
        self,
        name: str,
        fn: Callable,
    ) -> GraphBuilder:
        """Add an async function as a graph node.

        The function receives the current state dict and returns
        a partial state update dict.

        Args:
            name: Node name (used in edges and routing).
            fn: Async function(state) -> dict with state updates.

        Returns:
            self for chaining.
        """
        self._graph.add_node(name, fn)
        return self

    def add_crew_node(
        self,
        name: str,
        crew_factory: Callable[[Any], Any],
        *,
        inputs_fn: Callable[[Any], dict] | None = None,
    ) -> GraphBuilder:
        """Add a node that builds and executes a CrewAI crew.

        The crew_factory receives the current state and returns a Crew instance.
        The crew is then kicked off in a thread executor to avoid blocking
        the async event loop.

        Args:
            name: Node name.
            crew_factory: Callable(state) -> Crew. Builds the crew from state.
            inputs_fn: Optional callable(state) -> dict that extracts kickoff
                       inputs from the state. If None, no inputs are passed.

        Returns:
            self for chaining.
        """

        async def crew_node(state: dict) -> dict:
            crew = crew_factory(state)
            inputs = inputs_fn(state) if inputs_fn else None

            loop = asyncio.get_event_loop()

            def run_crew():
                if inputs:
                    return crew.kickoff(inputs=inputs)
                return crew.kickoff()

            result = await loop.run_in_executor(None, run_crew)
            return {"result": result.dict() if hasattr(result, "dict") else str(result)}

        self._graph.add_node(name, crew_node)
        return self

    def set_entry(self, node_name: str) -> GraphBuilder:
        """Set the entry point node.

        Args:
            node_name: Name of the first node to execute.

        Returns:
            self for chaining.
        """
        self._graph.add_edge(START, node_name)
        return self

    def add_edge(self, from_node: str, to_node: str) -> GraphBuilder:
        """Add a fixed edge between two nodes.

        Args:
            from_node: Source node name.
            to_node: Destination node name.

        Returns:
            self for chaining.
        """
        self._graph.add_edge(from_node, to_node)
        return self

    def add_terminal(self, node_name: str) -> GraphBuilder:
        """Add an edge from a node to END.

        Args:
            node_name: Node that terminates the graph.

        Returns:
            self for chaining.
        """
        self._graph.add_edge(node_name, END)
        return self

    def add_router(
        self,
        source_node: str,
        router_fn: Callable[[Any], str],
        route_map: dict[str, str],
    ) -> GraphBuilder:
        """Add conditional routing from a source node.

        Args:
            source_node: Node whose output is evaluated by router_fn.
            router_fn: Callable(state) -> str returning a route key.
            route_map: Maps route keys to destination node names.

        Returns:
            self for chaining.
        """
        self._graph.add_conditional_edges(source_node, router_fn, route_map)
        return self

    def build(self) -> Any:
        """Compile and return the LangGraph runnable.

        Returns:
            Compiled StateGraph that supports ainvoke(), invoke(), astream().
        """
        return self._graph.compile()

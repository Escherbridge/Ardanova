"""
Project Analysis Workflow

LangGraph StateGraph that orchestrates the project analysis endpoint.
Routes between a full multi-agent crew analysis (for complex projects)
and a quick single-pass analysis (for straightforward ones).

Graph:
    [START] → classify → route
                          ├→ full_analysis (complex)
                          └→ quick_analysis (simple)
              → finalize → [END]
"""

from __future__ import annotations

from typing import Any, Literal

from ..core.agent_builder import AgentBuilder
from ..core.graph_builder import GraphBuilder
from ..core.state import ProjectAnalysisState
from ..crews.project_analysis_crew import build_project_analysis_crew


async def classify_project(state: ProjectAnalysisState) -> dict[str, Any]:
    """Classify project complexity to determine routing.

    Simple heuristic for now - can be replaced with an LLM classifier later.
    Projects are "complex" if they have resources/milestones or long timelines.
    """
    resources = state.get("resources") or []
    milestones = state.get("milestones") or []
    timeline = state.get("timeline", "")

    is_complex = (
        len(resources) > 2
        or len(milestones) > 2
        or timeline in ("SIX_TWELVE_MONTHS", "ONE_TWO_YEARS", "TWO_PLUS_YEARS")
    )

    return {"complexity": "complex" if is_complex else "simple"}


def route_by_complexity(
    state: ProjectAnalysisState,
) -> Literal["full_analysis", "quick_analysis"]:
    """Router function: dispatch based on classified complexity."""
    return "full_analysis" if state.get("complexity") == "complex" else "quick_analysis"


async def run_full_analysis(state: ProjectAnalysisState) -> dict[str, Any]:
    """Execute the full multi-agent project analysis crew.

    Builds and kicks off the project analysis crew with three agents:
    viability analyst, market researcher, and financial projector.
    """
    # TODO: Replace with actual crew execution when LLM keys are configured.
    # The crew is built but not kicked off since the agents aren't functional yet.
    #
    # When ready, this will be:
    #   crew = build_project_analysis_crew(agent_builder, state)
    #   result = await asyncio.get_event_loop().run_in_executor(
    #       None, crew.kickoff
    #   )
    #   return parse_crew_result(result)

    return {
        "viability_score": 0.0,
        "viability_summary": "Full analysis not yet implemented",
        "success_score": 0.0,
        "success_rationale": "Pending agent implementation",
        "impact_score": 0.0,
        "impact_rationale": "Pending agent implementation",
        "profitability_score": 0.0,
        "profitability_rationale": "Pending agent implementation",
        "recommended_funding_goal": 0.0,
        "funding_rationale": "Pending agent implementation",
        "risks": ["Analysis agent not yet implemented"],
        "recommendations": ["Configure LLM API keys to enable analysis"],
    }


async def run_quick_analysis(state: ProjectAnalysisState) -> dict[str, Any]:
    """Execute a quick single-pass analysis for simple projects.

    Uses a lighter-weight approach for straightforward projects that
    don't need the full three-agent crew.
    """
    # TODO: Implement single-agent quick analysis.
    # This will use a single CrewAI agent with a combined prompt.

    return {
        "viability_score": 0.0,
        "viability_summary": "Quick analysis not yet implemented",
        "success_score": 0.0,
        "success_rationale": "Pending agent implementation",
        "impact_score": 0.0,
        "impact_rationale": "Pending agent implementation",
        "profitability_score": 0.0,
        "profitability_rationale": "Pending agent implementation",
        "recommended_funding_goal": 0.0,
        "funding_rationale": "Pending agent implementation",
        "risks": ["Analysis agent not yet implemented"],
        "recommendations": ["Configure LLM API keys to enable analysis"],
    }


async def finalize_result(state: ProjectAnalysisState) -> dict[str, Any]:
    """Finalize the analysis into the result field.

    Assembles the structured response from the crew outputs.
    Also handles updating the project's funding goal via MCP (TODO).
    """
    # TODO: Call MCP to update project funding goal
    # await mcp_client.call_tool("project_update", {
    #     "id": state["project_id"],
    #     "fundingGoal": state["recommended_funding_goal"],
    # })

    return {
        "result": {
            "project_id": state.get("project_id", ""),
            "viability": {
                "score": state.get("viability_score", 0.0),
                "summary": state.get("viability_summary", ""),
            },
            "success": {
                "score": state.get("success_score", 0.0),
                "rationale": state.get("success_rationale", ""),
            },
            "impact": {
                "score": state.get("impact_score", 0.0),
                "rationale": state.get("impact_rationale", ""),
            },
            "profitability": {
                "score": state.get("profitability_score", 0.0),
                "rationale": state.get("profitability_rationale", ""),
            },
            "funding_goal": {
                "recommended": state.get("recommended_funding_goal", 0.0),
                "rationale": state.get("funding_rationale", ""),
            },
            "risks": state.get("risks", []),
            "recommendations": state.get("recommendations", []),
        }
    }


def build_project_analysis_graph() -> Any:
    """Build and compile the project analysis LangGraph workflow.

    Returns:
        Compiled StateGraph that supports ainvoke() and astream().
    """
    graph = (
        GraphBuilder(ProjectAnalysisState)
        .add_function_node("classify", classify_project)
        .add_function_node("full_analysis", run_full_analysis)
        .add_function_node("quick_analysis", run_quick_analysis)
        .add_function_node("finalize", finalize_result)
        .set_entry("classify")
        .add_router(
            "classify",
            route_by_complexity,
            {"full_analysis": "full_analysis", "quick_analysis": "quick_analysis"},
        )
        .add_edge("full_analysis", "finalize")
        .add_edge("quick_analysis", "finalize")
        .add_terminal("finalize")
        .build()
    )

    return graph


# Compile the graph at module level for import
project_analysis_graph = build_project_analysis_graph()

"""
Shared state definitions for LangGraph workflows.

Each workflow extends BaseWorkflowState with domain-specific fields.
"""

from typing import Any, TypedDict


class BaseWorkflowState(TypedDict, total=False):
    """Common state fields shared across all LangGraph workflows."""

    # Input
    query: str
    intent: str

    # Processing metadata
    metadata: dict[str, Any]

    # Output
    result: dict[str, Any]
    error: str


class ProjectAnalysisState(TypedDict, total=False):
    """State for the project analysis LangGraph workflow."""

    # Project input data
    project_id: str
    title: str
    problem_statement: str
    solution: str
    category: str
    target_audience: str
    expected_impact: str
    timeline: str
    resources: list[dict[str, Any]]
    milestones: list[dict[str, Any]]

    # Routing
    complexity: str  # "simple" | "complex"

    # Crew outputs
    viability_score: float
    viability_summary: str
    success_score: float
    success_rationale: str
    impact_score: float
    impact_rationale: str
    profitability_score: float
    profitability_rationale: str
    recommended_funding_goal: float
    funding_rationale: str
    risks: list[str]
    recommendations: list[str]

    # Final
    result: dict[str, Any]
    error: str

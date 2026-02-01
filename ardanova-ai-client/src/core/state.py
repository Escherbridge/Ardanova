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


class PitchGenerationState(TypedDict, total=False):
    """State for the pitch generation LangGraph workflow.

    Flows through: parse_prompt → concept_crew → strategy_crew →
                   assemble → (optionally) generate_deck → finalize
    """

    # Input
    prompt: str  # User's base prompt describing the project idea
    user_id: str  # Authenticated user submitting the pitch
    generate_deck: bool  # Whether to generate a Gamma pitch deck
    partial_data: dict[str, Any]  # Pre-filled data for enhance mode

    # Phase 1: Concept (from concept crew)
    title: str
    problem_statement: str
    solution: str
    categories: list[str]
    project_type: str  # TEMPORARY, LONG_TERM, FOUNDATION, BUSINESS, PRODUCT, OPEN_SOURCE, COMMUNITY
    duration: str  # ONE_TWO_WEEKS through ONGOING

    # Phase 2: Strategy (from strategy crew)
    milestones: list[dict[str, Any]]  # [{title, description, target_date}]
    resources: list[dict[str, Any]]  # [{name, description, quantity, estimated_cost, is_required}]
    timeline: str  # human-readable timeline summary

    # Phase 3: Positioning (from concept crew, second pass)
    tags: list[str]
    target_audience: str
    expected_impact: str
    description: str  # polished project description

    # Phase 4: Pitch deck (optional)
    deck_url: str  # Gamma presentation URL
    deck_generation_id: str

    # Assembly
    project_data: dict[str, Any]  # Full assembled project ready for API submission
    result: dict[str, Any]
    error: str

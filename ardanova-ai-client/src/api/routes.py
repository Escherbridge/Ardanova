"""
API Routes

Route definitions for the AI orchestrator service.
Each endpoint delegates to a LangGraph workflow.
"""

from typing import Any

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

from ..workflows.pitch_generation import deck_only_graph, pitch_generation_graph
from ..workflows.project_analysis import project_analysis_graph

router = APIRouter(prefix="/api/agent", tags=["agent"])


class ProjectAnalysisRequest(BaseModel):
    """Request body for project analysis endpoint"""

    project_id: str
    title: str
    problem_statement: str
    solution: str
    category: str
    target_audience: str | None = None
    expected_impact: str | None = None
    timeline: str | None = None
    resources: list[dict] | None = None
    milestones: list[dict] | None = None


@router.post("/analyze-project")
async def analyze_project(request: ProjectAnalysisRequest):
    """Analyze a project for viability and generate a funding goal.

    Delegates to the project analysis LangGraph workflow which routes
    between a full multi-agent crew (complex projects) and a quick
    single-pass analysis (simple projects).

    Called after project creation to produce:
    - Viability assessment
    - Success/impact/profitability projections
    - Dynamically generated funding goal
    """
    initial_state = {
        "project_id": request.project_id,
        "title": request.title,
        "problem_statement": request.problem_statement,
        "solution": request.solution,
        "category": request.category,
        "target_audience": request.target_audience or "",
        "expected_impact": request.expected_impact or "",
        "timeline": request.timeline or "",
        "resources": request.resources or [],
        "milestones": request.milestones or [],
    }

    try:
        result = await project_analysis_graph.ainvoke(initial_state)
        return result.get("result", {})
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Project analysis workflow failed: {e}",
        )


# ── Pitch Generation Endpoints ─────────────────────────────────────────


class PitchGenerateRequest(BaseModel):
    """Request body for full end-to-end pitch generation.

    Takes a single prompt and produces a complete project with
    milestones, resources, categories, tags, and optionally a
    Gamma pitch deck.
    """

    prompt: str = Field(..., min_length=10, description="Project idea description")
    user_id: str = Field(..., description="Authenticated user ID")
    generate_deck: bool = Field(
        default=False, description="Whether to generate a Gamma pitch deck"
    )


class PitchEnhanceRequest(BaseModel):
    """Request body for enhancing partial project data.

    Use when the user has already filled in some fields via the
    multipart form and wants AI to fill in the rest.
    """

    prompt: str = Field(..., min_length=10, description="Project idea description")
    user_id: str = Field(..., description="Authenticated user ID")
    generate_deck: bool = Field(default=False)
    partial_data: dict[str, Any] = Field(
        default_factory=dict,
        description="Pre-filled project fields to preserve (title, categories, etc.)",
    )


class PitchDeckRequest(BaseModel):
    """Request body for generating a pitch deck from existing project data.

    Use when a project already exists and the user just wants a
    Gamma presentation generated from the project's data.
    """

    title: str
    description: str = ""
    problem_statement: str = ""
    solution: str = ""
    categories: list[str] = Field(default_factory=list)
    project_type: str = "PRODUCT"
    target_audience: str = ""
    expected_impact: str = ""
    milestones: list[dict[str, Any]] = Field(default_factory=list)
    resources: list[dict[str, Any]] = Field(default_factory=list)
    tags: list[str] = Field(default_factory=list)


@router.post("/pitch/generate")
async def generate_pitch(request: PitchGenerateRequest):
    """Generate a complete project pitch from a single prompt.

    Runs the full pipeline:
        parse_prompt → concept_crew → strategy_crew → positioning_crew
        → assemble → (optionally) generate_deck → finalize

    Returns a complete project payload matching CreateProjectDto
    plus optional deck URL.
    """
    initial_state = {
        "prompt": request.prompt,
        "user_id": request.user_id,
        "generate_deck": request.generate_deck,
    }

    try:
        result = await pitch_generation_graph.ainvoke(initial_state)
        return result.get("result", {})
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Pitch generation workflow failed: {e}",
        )


@router.post("/pitch/enhance")
async def enhance_pitch(request: PitchEnhanceRequest):
    """Enhance partial project data with AI-generated content.

    Accepts pre-filled fields via partial_data. The workflow
    merges these into state and skips crews whose output fields
    are already populated. Only generates missing data.

    Use this when the user has partially filled the multipart form
    and wants AI to complete the rest.
    """
    initial_state = {
        "prompt": request.prompt,
        "user_id": request.user_id,
        "generate_deck": request.generate_deck,
        "partial_data": request.partial_data,
    }

    try:
        result = await pitch_generation_graph.ainvoke(initial_state)
        return result.get("result", {})
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Pitch enhancement workflow failed: {e}",
        )


@router.post("/pitch/deck")
async def generate_deck(request: PitchDeckRequest):
    """Generate a Gamma pitch deck from existing project data.

    Skips the crew pipeline entirely and goes straight to
    assembly → deck generation → finalize.

    Use this for projects that already have all their data
    and just need a presentation.
    """
    initial_state = {
        "title": request.title,
        "description": request.description,
        "problem_statement": request.problem_statement,
        "solution": request.solution,
        "categories": request.categories,
        "project_type": request.project_type,
        "target_audience": request.target_audience,
        "expected_impact": request.expected_impact,
        "milestones": request.milestones,
        "resources": request.resources,
        "tags": request.tags,
        "generate_deck": True,  # Always true for this endpoint
    }

    try:
        result = await deck_only_graph.ainvoke(initial_state)
        return result.get("result", {})
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Deck generation workflow failed: {e}",
        )

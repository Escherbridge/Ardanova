"""
API Routes

Route definitions for the AI orchestrator service.
Each endpoint delegates to a LangGraph workflow.
"""

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

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

"""
Project Analysis Crew

CrewAI crew that analyzes a submitted project for viability,
market fit, and financial projections to generate a recommended
funding goal.

Uses CrewBuilder to compose three specialized agents:
1. Viability Analyst - problem/solution fit assessment
2. Market Researcher - category benchmarks and competition analysis
3. Financial Projector - funding goal calculation
"""

from __future__ import annotations

from typing import Any

from crewai import Crew

from ..core.agent_builder import AgentBuilder
from ..core.crew_builder import CrewBuilder


def build_project_analysis_crew(
    agent_builder: AgentBuilder,
    project_data: dict[str, Any] | None = None,
) -> Crew:
    """Build a crew for comprehensive project analysis.

    This crew runs three agents in sequence:
    1. Viability Analyst evaluates the problem/solution fit
    2. Market Researcher analyzes category benchmarks and competition
    3. Financial Projector synthesizes findings into a funding goal

    Args:
        agent_builder: AgentBuilder with LLM and MCP configured.
        project_data: Optional project data dict for interpolation into
                      task descriptions. If None, uses {placeholders}.

    Returns:
        A Crew ready for kickoff(inputs={"project_title": ..., ...}).
    """
    title = project_data.get("title", "{title}") if project_data else "{title}"
    problem = (
        project_data.get("problem_statement", "{problem_statement}")
        if project_data
        else "{problem_statement}"
    )
    solution = (
        project_data.get("solution", "{solution}") if project_data else "{solution}"
    )
    category = (
        project_data.get("category", "{category}") if project_data else "{category}"
    )
    audience = (
        project_data.get("target_audience", "{target_audience}")
        if project_data
        else "{target_audience}"
    )
    impact = (
        project_data.get("expected_impact", "{expected_impact}")
        if project_data
        else "{expected_impact}"
    )
    timeline = (
        project_data.get("timeline", "{timeline}") if project_data else "{timeline}"
    )

    crew = (
        CrewBuilder(agent_builder)
        # Agent 0: Viability Analyst
        .add_agent(
            role="Project Viability Analyst",
            goal=(
                "Assess the viability of submitted projects by evaluating "
                "problem/solution fit, team capacity, and execution feasibility"
            ),
            backstory=(
                "You are a seasoned project evaluator with 15 years of experience "
                "reviewing startup pitches and project proposals. You've evaluated "
                "thousands of projects across technology, social impact, and business "
                "domains. You focus on whether the problem is real, the solution is "
                "sound, and the team can deliver."
            ),
            tool_domains=["project"],
        )
        # Agent 1: Market Researcher
        .add_agent(
            role="Market Research Analyst",
            goal=(
                "Research market conditions, category benchmarks, and competitive "
                "landscape to assess the project's potential for success and impact"
            ),
            backstory=(
                "You are an expert market researcher who specializes in analyzing "
                "emerging markets and project categories. You understand market sizing, "
                "competitive dynamics, and what makes projects succeed in their niche. "
                "You reference similar successful projects and industry benchmarks."
            ),
            tool_domains=["project"],
        )
        # Agent 2: Financial Projector
        .add_agent(
            role="Financial Projection Specialist",
            goal=(
                "Calculate a realistic funding goal and financial projections "
                "based on the viability assessment and market research"
            ),
            backstory=(
                "You are a financial analyst who specializes in project funding "
                "and resource estimation. You understand how to translate project "
                "scope, timeline, and resource needs into concrete dollar amounts. "
                "You factor in team costs, infrastructure, marketing, contingency, "
                "and expected returns to arrive at a recommended funding goal."
            ),
            tool_domains=["project"],
        )
        # Task 0: Viability Assessment (Agent 0)
        .add_task(
            description=(
                f"Evaluate the viability of the following project:\n\n"
                f"Title: {title}\n"
                f"Problem: {problem}\n"
                f"Solution: {solution}\n"
                f"Category: {category}\n"
                f"Target Audience: {audience}\n"
                f"Expected Impact: {impact}\n"
                f"Timeline: {timeline}\n\n"
                "Assess:\n"
                "1. Problem significance (is this a real, important problem?)\n"
                "2. Solution quality (does the solution address the problem well?)\n"
                "3. Execution feasibility (can this realistically be built?)\n"
                "4. Key risks and concerns\n\n"
                "Provide a viability score from 0.0 to 1.0."
            ),
            expected_output=(
                "A structured viability assessment with:\n"
                "- viability_score: float between 0.0 and 1.0\n"
                "- viability_summary: 2-3 sentence summary\n"
                "- key risks identified\n"
                "- recommendations for improvement"
            ),
            agent_index=0,
        )
        # Task 1: Market Analysis (Agent 1, depends on Task 0)
        .add_task(
            description=(
                f"Research the market conditions for a {category} project "
                f"targeting {audience}.\n\n"
                "Based on the viability assessment, analyze:\n"
                "1. Market size and growth potential\n"
                "2. Similar projects and their funding levels\n"
                "3. Success rate for projects in this category\n"
                "4. Impact potential given the target audience\n"
                "5. Profitability outlook\n\n"
                "Provide success, impact, and profitability scores (0.0 to 1.0)."
            ),
            expected_output=(
                "A structured market analysis with:\n"
                "- success_score: float between 0.0 and 1.0 with rationale\n"
                "- impact_score: float between 0.0 and 1.0 with rationale\n"
                "- profitability_score: float between 0.0 and 1.0 with rationale\n"
                "- comparable project funding benchmarks"
            ),
            agent_index=1,
            context_tasks=[0],
        )
        # Task 2: Funding Projection (Agent 2, depends on Tasks 0 & 1)
        .add_task(
            description=(
                "Based on the viability assessment and market analysis, "
                "calculate a recommended funding goal for this project.\n\n"
                "Consider:\n"
                "1. Resource requirements (team, infrastructure, tools)\n"
                "2. Timeline and burn rate\n"
                "3. Marketing and user acquisition costs\n"
                "4. Contingency buffer (typically 15-25%)\n"
                "5. Category benchmarks from the market analysis\n"
                "6. Viability and success scores\n\n"
                "Provide a specific dollar amount with detailed breakdown."
            ),
            expected_output=(
                "A funding recommendation with:\n"
                "- recommended_funding_goal: specific USD amount\n"
                "- funding_rationale: detailed breakdown of how the goal was calculated\n"
                "- consolidated risks list\n"
                "- consolidated recommendations list"
            ),
            agent_index=2,
            context_tasks=[0, 1],
        )
        .build(process="sequential")
    )

    return crew

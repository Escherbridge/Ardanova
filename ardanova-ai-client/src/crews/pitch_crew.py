"""
Pitch Generation Crews

CrewAI crews for generating complete project pitches from a single prompt.
Split into focused crews that can be used independently or chained via LangGraph:

1. ConceptCrew - Generates project concept (title, problem, solution, categories)
2. StrategyCrew - Creates milestones, resources, timeline
3. PositioningCrew - Generates tags, target audience, impact, polished description
"""

from __future__ import annotations

from typing import Any

from crewai import Crew

from ..core.agent_builder import AgentBuilder
from ..core.crew_builder import CrewBuilder


def build_concept_crew(agent_builder: AgentBuilder) -> Crew:
    """Build a crew that generates the core project concept from a prompt.

    Agents:
    - Concept Architect: Creates title, problem statement, solution
    - Category Analyst: Determines categories, project type, duration

    Output: title, problem_statement, solution, categories, project_type, duration
    """
    return (
        CrewBuilder(agent_builder)
        .add_agent(
            role="Project Concept Architect",
            goal=(
                "Transform a raw project idea into a compelling, structured "
                "project concept with a clear problem statement and solution"
            ),
            backstory=(
                "You are a serial entrepreneur and startup advisor who has helped "
                "launch over 200 projects. You excel at taking rough ideas and "
                "distilling them into clear problem/solution narratives. You know "
                "what makes investors and supporters pay attention: specific problems "
                "with measurable impact, and solutions that are both innovative and "
                "feasible. You write concisely but compellingly."
            ),
            tool_domains=["project"],
        )
        .add_agent(
            role="Project Classification Specialist",
            goal=(
                "Accurately categorize projects and determine the optimal project "
                "type and expected duration based on the concept"
            ),
            backstory=(
                "You are a portfolio manager who has classified thousands of projects "
                "across technology, healthcare, education, environment, social impact, "
                "business, arts, agriculture, and finance sectors. You understand the "
                "nuances between project types (temporary, long-term, foundation, "
                "business, product, open source, community) and can estimate realistic "
                "durations based on scope and complexity."
            ),
            tool_domains=["project"],
        )
        .add_task(
            description=(
                "Given this project idea: {prompt}\n\n"
                "Create a structured project concept with:\n"
                "1. A compelling, concise project title (max 80 characters)\n"
                "2. A problem statement (2-4 sentences) explaining what problem "
                "this project solves and who it affects\n"
                "3. A proposed solution (2-4 sentences) explaining how this project "
                "addresses the problem, what makes it unique\n\n"
                "Be specific, avoid jargon, and focus on impact."
            ),
            expected_output=(
                "Structured output with exactly these fields:\n"
                "- title: string (max 80 chars)\n"
                "- problem_statement: string (2-4 sentences)\n"
                "- solution: string (2-4 sentences)"
            ),
            agent_index=0,
        )
        .add_task(
            description=(
                "Based on the project concept above, determine:\n\n"
                "1. Categories - select 1-3 from: TECHNOLOGY, HEALTHCARE, EDUCATION, "
                "ENVIRONMENT, SOCIAL_IMPACT, BUSINESS, ARTS_CULTURE, AGRICULTURE, "
                "FINANCE, OTHER. If OTHER, provide a custom category name (max 50 chars).\n"
                "2. Project Type - select one: TEMPORARY, LONG_TERM, FOUNDATION, "
                "BUSINESS, PRODUCT, OPEN_SOURCE, COMMUNITY\n"
                "3. Duration - select one: ONE_TWO_WEEKS, ONE_THREE_MONTHS, "
                "THREE_SIX_MONTHS, SIX_TWELVE_MONTHS, ONE_TWO_YEARS, "
                "TWO_PLUS_YEARS, ONGOING\n\n"
                "Base your decisions on the project's scope, complexity, and nature."
            ),
            expected_output=(
                "Structured output with exactly these fields:\n"
                "- categories: list of 1-3 category strings\n"
                "- project_type: one of the ProjectType enum values\n"
                "- duration: one of the ProjectDuration enum values"
            ),
            agent_index=1,
            context_tasks=[0],
        )
        .build()
    )


def build_strategy_crew(agent_builder: AgentBuilder) -> Crew:
    """Build a crew that creates the project execution strategy.

    Agents:
    - Milestone Planner: Defines project milestones with target dates
    - Resource Planner: Identifies required resources and costs

    Output: milestones, resources, timeline summary
    """
    return (
        CrewBuilder(agent_builder)
        .add_agent(
            role="Project Milestone Planner",
            goal=(
                "Create a realistic, actionable milestone plan that breaks the "
                "project into clear phases with measurable deliverables"
            ),
            backstory=(
                "You are a project management expert certified in PMP and Agile. "
                "You've planned timelines for projects ranging from 2-week sprints "
                "to multi-year initiatives. You understand that good milestones are "
                "specific, measurable, and have realistic target dates. You always "
                "include a kickoff milestone and a completion/launch milestone."
            ),
        )
        .add_agent(
            role="Resource Planning Analyst",
            goal=(
                "Identify all resources needed to execute the project and estimate "
                "their costs realistically"
            ),
            backstory=(
                "You are a resource planning specialist who has budgeted hundreds of "
                "projects. You think about people, technology, infrastructure, "
                "services, and operational costs. You categorize resources as required "
                "or optional, include recurring costs where applicable, and always "
                "add a contingency buffer. Your estimates are grounded in market rates."
            ),
        )
        .add_task(
            description=(
                "Create a milestone plan for this project:\n\n"
                "Title: {title}\n"
                "Problem: {problem_statement}\n"
                "Solution: {solution}\n"
                "Duration: {duration}\n\n"
                "Generate 3-6 milestones, each with:\n"
                "- title: concise milestone name\n"
                "- description: what this milestone delivers (1-2 sentences)\n"
                "- target_date: ISO date string (YYYY-MM-DD), spaced across the "
                "project duration starting from today\n\n"
                "First milestone should be project kickoff, last should be "
                "launch/completion."
            ),
            expected_output=(
                "A list of 3-6 milestones, each with:\n"
                "- title: string\n"
                "- description: string\n"
                "- target_date: string (YYYY-MM-DD format)"
            ),
            agent_index=0,
        )
        .add_task(
            description=(
                "Identify the resources needed for this project:\n\n"
                "Title: {title}\n"
                "Solution: {solution}\n"
                "Duration: {duration}\n\n"
                "Based on the milestones above, identify 2-5 key resources with:\n"
                "- name: resource name (e.g., 'Cloud Hosting', 'UI/UX Designer')\n"
                "- description: what it's used for (1 sentence)\n"
                "- quantity: number needed (default 1)\n"
                "- estimated_cost: cost in USD\n"
                "- is_required: boolean (true for must-have, false for nice-to-have)\n\n"
                "Also provide a human-readable timeline summary (1-2 sentences)."
            ),
            expected_output=(
                "Structured output with:\n"
                "- resources: list of 2-5 resource objects\n"
                "- timeline: string summary of the project timeline"
            ),
            agent_index=1,
            context_tasks=[0],
        )
        .build()
    )


def build_positioning_crew(agent_builder: AgentBuilder) -> Crew:
    """Build a crew that handles project positioning and polish.

    Agents:
    - Content Strategist: Creates tags, target audience, expected impact
    - Description Writer: Crafts the polished project description

    Output: tags, target_audience, expected_impact, description
    """
    return (
        CrewBuilder(agent_builder)
        .add_agent(
            role="Project Positioning Strategist",
            goal=(
                "Define the target audience, expected impact, and discoverability "
                "tags that will maximize the project's visibility and support"
            ),
            backstory=(
                "You are a marketing strategist who specializes in project positioning "
                "on crowdfunding and social impact platforms. You understand that the "
                "right tags, audience definition, and impact framing can make the "
                "difference between a project getting noticed or being buried. You "
                "choose tags that are specific, searchable, and relevant."
            ),
        )
        .add_agent(
            role="Project Description Writer",
            goal=(
                "Write a compelling, polished project description that synthesizes "
                "all project elements into a cohesive narrative"
            ),
            backstory=(
                "You are a copywriter who specializes in project pitches and "
                "campaign pages. You know how to take technical details and "
                "translate them into engaging prose that resonates with supporters, "
                "investors, and collaborators. Your descriptions are concise but "
                "complete, professional but approachable."
            ),
        )
        .add_task(
            description=(
                "Determine positioning for this project:\n\n"
                "Title: {title}\n"
                "Problem: {problem_statement}\n"
                "Solution: {solution}\n"
                "Categories: {categories}\n\n"
                "Generate:\n"
                "1. Tags: 3-8 relevant, searchable tags (lowercase, no spaces, "
                "use hyphens)\n"
                "2. Target Audience: 1-2 sentences describing who benefits most\n"
                "3. Expected Impact: 2-3 sentences on measurable outcomes"
            ),
            expected_output=(
                "Structured output with:\n"
                "- tags: list of 3-8 tag strings\n"
                "- target_audience: string (1-2 sentences)\n"
                "- expected_impact: string (2-3 sentences)"
            ),
            agent_index=0,
        )
        .add_task(
            description=(
                "Write a polished project description that synthesizes:\n\n"
                "Title: {title}\n"
                "Problem: {problem_statement}\n"
                "Solution: {solution}\n"
                "Target Audience: from positioning analysis above\n"
                "Expected Impact: from positioning analysis above\n\n"
                "The description should be 3-5 paragraphs, covering:\n"
                "- What the project does and why it matters\n"
                "- How it works (the solution approach)\n"
                "- Who it helps and what impact it will have\n"
                "- Why now and what makes this team/approach unique\n\n"
                "Write in third person, professional but engaging tone."
            ),
            expected_output=(
                "A polished project description string, 3-5 paragraphs."
            ),
            agent_index=1,
            context_tasks=[0],
        )
        .build()
    )

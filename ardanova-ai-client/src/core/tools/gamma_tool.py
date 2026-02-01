"""
Gamma API Tool

CrewAI-compatible tool for generating pitch decks and presentations
via the Gamma API (https://public-api.gamma.app/v1.0).

Gamma is an AI-powered content creation platform that generates
presentations, documents, and web pages from text prompts.
"""

from __future__ import annotations

import time
from typing import Any, Literal, Type

import httpx
from crewai.tools import BaseTool
from pydantic import BaseModel, Field


class GammaGenerateInput(BaseModel):
    """Input schema for the Gamma generation tool."""

    input_text: str = Field(
        ...,
        description=(
            "The content/prompt for generating the presentation. "
            "Can be a detailed pitch outline or a brief topic description."
        ),
    )
    format: str = Field(
        default="presentation",
        description="Type of content: presentation, document, social, or webpage",
    )
    text_mode: str = Field(
        default="generate",
        description="How to process input: generate (expand), condense (summarize), preserve (exact)",
    )
    num_cards: int = Field(
        default=10,
        description="Number of slides/cards (1-60)",
    )
    additional_instructions: str | None = Field(
        default=None,
        description="Extra styling or content instructions (max 2000 chars)",
    )
    export_as: str | None = Field(
        default=None,
        description="Export format: pdf or pptx (optional)",
    )


class GammaGenerateTool(BaseTool):
    """CrewAI tool that generates presentations via the Gamma API.

    Submits a generation request, polls for completion, and returns
    the URL to the generated content.

    Requires GAMMA_API_KEY in settings. Returns a stub response
    if the key is not configured.
    """

    name: str = "gamma_generate_presentation"
    description: str = (
        "Generates AI-powered presentations and pitch decks using Gamma. "
        "Takes a text prompt describing the content and returns a URL to "
        "the generated presentation. Supports presentations, documents, "
        "social posts, and web pages."
    )
    args_schema: Type[BaseModel] = GammaGenerateInput
    api_key: str = ""
    base_url: str = "https://public-api.gamma.app/v1.0"

    model_config = {"arbitrary_types_allowed": True}

    def _run(
        self,
        input_text: str,
        format: str = "presentation",
        text_mode: str = "generate",
        num_cards: int = 10,
        additional_instructions: str | None = None,
        export_as: str | None = None,
    ) -> str:
        """Generate content via Gamma API."""
        if not self.api_key:
            return (
                "Gamma API key not configured. "
                "Set GAMMA_API_KEY environment variable to enable pitch deck generation. "
                "A Gamma Pro subscription ($18/month) is required for API access."
            )

        headers = {
            "Content-Type": "application/json",
            "X-API-KEY": self.api_key,
        }

        payload: dict[str, Any] = {
            "inputText": input_text,
            "textMode": text_mode,
            "format": format,
            "numCards": num_cards,
        }

        if additional_instructions:
            payload["additionalInstructions"] = additional_instructions
        if export_as:
            payload["exportAs"] = export_as

        try:
            # Submit generation request
            with httpx.Client(timeout=30.0) as client:
                response = client.post(
                    f"{self.base_url}/generations",
                    headers=headers,
                    json=payload,
                )
                response.raise_for_status()
                result = response.json()

            generation_id = result.get("generationId")
            if not generation_id:
                return f"Error: No generation ID returned. Response: {result}"

            # Poll for completion (max 5 minutes, 5s intervals)
            with httpx.Client(timeout=30.0) as client:
                for _ in range(60):
                    time.sleep(5)
                    status_response = client.get(
                        f"{self.base_url}/generations/{generation_id}",
                        headers=headers,
                    )
                    status_response.raise_for_status()
                    status_data = status_response.json()

                    if status_data["status"] == "completed":
                        gamma_url = status_data.get("gammaUrl", "")
                        credits = status_data.get("credits", {})
                        return (
                            f"Presentation generated successfully.\n"
                            f"URL: {gamma_url}\n"
                            f"Credits used: {credits.get('deducted', 'N/A')}\n"
                            f"Credits remaining: {credits.get('remaining', 'N/A')}"
                        )
                    elif status_data["status"] == "failed":
                        return f"Generation failed: {status_data}"

            return f"Generation timed out. Check manually with ID: {generation_id}"

        except httpx.HTTPStatusError as e:
            if e.response.status_code == 403:
                return "Insufficient credits or unauthorized. Check Gamma subscription."
            elif e.response.status_code == 400:
                return f"Invalid request: {e.response.text}"
            return f"HTTP error {e.response.status_code}: {e.response.text}"
        except Exception as e:
            return f"Gamma API error: {e}"


class GammaStatusInput(BaseModel):
    """Input schema for checking Gamma generation status."""

    generation_id: str = Field(..., description="The generation ID to check status for")


class GammaStatusTool(BaseTool):
    """Check the status of a Gamma generation request."""

    name: str = "gamma_check_status"
    description: str = (
        "Check the status of a previously submitted Gamma generation. "
        "Returns the current status and URL if completed."
    )
    args_schema: Type[BaseModel] = GammaStatusInput
    api_key: str = ""
    base_url: str = "https://public-api.gamma.app/v1.0"

    model_config = {"arbitrary_types_allowed": True}

    def _run(self, generation_id: str) -> str:
        """Check generation status."""
        if not self.api_key:
            return "Gamma API key not configured."

        headers = {"X-API-KEY": self.api_key}

        try:
            with httpx.Client(timeout=30.0) as client:
                response = client.get(
                    f"{self.base_url}/generations/{generation_id}",
                    headers=headers,
                )
                response.raise_for_status()
                data = response.json()

            status = data.get("status", "unknown")
            if status == "completed":
                return f"Completed. URL: {data.get('gammaUrl', 'N/A')}"
            elif status == "failed":
                return f"Failed: {data}"
            return f"Status: {status}"
        except Exception as e:
            return f"Error checking status: {e}"

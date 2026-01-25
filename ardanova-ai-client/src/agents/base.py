"""
Base Agent Class

All agents inherit from this base class.
"""

from abc import ABC, abstractmethod
from typing import Any


class BaseAgent(ABC):
    """Base class for all AI agents"""

    name: str
    description: str
    capabilities: list[str]

    @abstractmethod
    async def process(self, message: str, context: dict[str, Any]) -> str:
        """Process a user message and return a response"""
        pass

    @abstractmethod
    async def stream(self, message: str, context: dict[str, Any]):
        """Stream a response to the user"""
        pass

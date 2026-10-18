"""Service-key guard for the agent routes; fail closed when INBOUND_API_KEY is unset."""

import hmac

from fastapi import Header, HTTPException

from ..config.settings import settings


async def require_service_key(x_api_key: str | None = Header(default=None)) -> None:
    """Admit only callers presenting INBOUND_API_KEY as X-Api-Key (constant-time compare)."""
    expected = settings.inbound_api_key
    if not expected:
        raise HTTPException(status_code=503, detail="Agent routes are disabled")
    if not x_api_key or not hmac.compare_digest(x_api_key.encode(), expected.encode()):
        raise HTTPException(status_code=401, detail="Unauthorized")

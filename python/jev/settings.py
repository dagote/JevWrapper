"""Configuration for a Jev client connection."""

from dataclasses import dataclass


@dataclass
class JevSettings:
    """Connection settings for one JevClient."""
    
    endpoint: str = "https://www.dagote.ai/api/jev/v1/systemone"
    api_key: str = ""
    model: str = "jev-latest"
    timeout_seconds: int = 30

    def clone(self) -> "JevSettings":
        """Create a copy of these settings."""
        return JevSettings(
            endpoint=self.endpoint,
            api_key=self.api_key,
            model=self.model,
            timeout_seconds=self.timeout_seconds,
        )

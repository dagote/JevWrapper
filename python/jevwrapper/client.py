"""Main Jev client for making requests to the Jev endpoint."""

import json
import logging
from typing import Callable, Optional

import requests

from .context import JevContext
from .protocol import build_request_json, build_state_text, parse_answer
from .settings import JevSettings
from .types import JevAnswer, JevQuestions

logger = logging.getLogger(__name__)


class JevClient:
    """One connection to Jev with its own settings and signal context.
    
    Give each system its own client so they don't share a model, key or groups.
    """

    def __init__(self, settings: Optional[JevSettings] = None):
        """Initialize a Jev client.
        
        Args:
            settings: Connection settings. If None, uses defaults.
        """
        self.settings = settings or JevSettings()
        self.context = JevContext()
        self._in_flight = set()

    def update(self, name: str, group: str, value: float) -> None:
        """Update a signal value.
        
        Args:
            name: Signal name
            group: Group name (required)
            value: Signal value
        """
        self.context.update(name, group, value)

    def question(
        self,
        group: str,
        questions: JevQuestions,
        on_answer: Callable[[JevAnswer], None],
        state_text: Optional[str] = None,
    ) -> bool:
        """Ask a question and get an answer.
        
        This is a synchronous blocking call that makes an HTTP request to the
        Jev endpoint. For async operation, run this in a thread.
        
        Args:
            group: Group name (required, used to track signals)
            questions: Questions to ask
            on_answer: Callback function that receives the JevAnswer
            state_text: Optional prose describing the situation. If None, signals
                       from the group are rendered as "name is value" sentences.
        
        Returns:
            False if arguments are invalid or a request for this group is already
            in flight. True if the request was started (callback will be called).
        """
        if not group or not questions or not on_answer:
            return False
        
        if group in self._in_flight:
            return False
        
        self._in_flight.add(group)
        
        try:
            if state_text is None:
                state_text = build_state_text(self.context.snapshot(group))
            
            json_body = build_request_json(self.settings.model, state_text, questions)
            
            headers = {
                "Content-Type": "application/json",
            }
            
            if self.settings.api_key:
                headers["x-api-key"] = self.settings.api_key
            
            response = requests.post(
                self.settings.endpoint,
                data=json_body,
                headers=headers,
                timeout=self.settings.timeout_seconds,
            )
            
            answer = JevAnswer(group=group)
            
            if response.status_code == 200:
                parse_answer(response.text, answer)
            else:
                answer.error = f"HTTP {response.status_code}: {response.text}"
                logger.warning(
                    f"Jev request failed: {response.status_code}\n{response.text}"
                )
            
            on_answer(answer)
            return True
            
        except requests.RequestException as e:
            answer = JevAnswer(group=group, error=str(e))
            logger.warning(f"Jev request failed: {e}")
            on_answer(answer)
            return True
            
        finally:
            self._in_flight.discard(group)

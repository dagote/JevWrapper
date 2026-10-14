"""Request building and response parsing for Jev protocol.

This module is kept free of networking so it can be tested independently.
"""

import json
import logging
from typing import Dict
from .types import JevQuestions, JevAnswer, JevAnswerItem, QuestionType

logger = logging.getLogger(__name__)


def build_state_text(state: Dict[str, float]) -> str:
    """Convert a state dictionary to prose description.
    
    Args:
        state: Dictionary of signal name to float value
        
    Returns:
        Human-readable state description like "health is 10. mana is 5."
    """
    if not state:
        return ""
    
    parts = [f"{name} is {value:g}." for name, value in state.items()]
    return " ".join(parts)


def build_request_json(
    model: str, state_text: str, questions: JevQuestions
) -> str:
    """Build the JSON request body for Jev.
    
    Args:
        model: Model name (e.g., "jev-latest")
        state_text: Prose description of the current state
        questions: Questions to ask the model
        
    Returns:
        JSON string ready for POST
    """
    questions_obj = {}
    
    for q in questions.items:
        question = {
            "type": q.type.value,
            "instructions": q.instructions or "",
        }
        
        if q.type in (QuestionType.CHOICE, QuestionType.SCORE):
            criteria = {}
            if q.options:
                for opt in q.options:
                    # Use description if available, otherwise use option name as description
                    criteria[opt] = q.criteria.get(opt, opt)
            question["criteria"] = criteria
        
        questions_obj[q.name] = question
    
    root = {
        "model": model,
        "state": state_text,
        "questions": questions_obj,
    }
    
    return json.dumps(root, separators=(",", ":"))


def parse_answer(json_str: str, answer: JevAnswer) -> None:
    """Parse a JSON response into a JevAnswer object.
    
    Args:
        json_str: JSON response from Jev endpoint
        answer: JevAnswer object to populate
    """
    answer.raw_json = json_str
    
    try:
        data = json.loads(json_str)
        answers_obj = data.get("answers")
        
        if answers_obj is None:
            answer.error = "response has no 'answers'"
            return
        
        for name, a in answers_obj.items():
            try:
                type_str = a.get("type", "").lower()
                question_type = QuestionType(type_str) if type_str else None
            except ValueError:
                # Unknown type
                question_type = None
            
            item = JevAnswerItem(
                name=name,
                type=question_type,
                choice=a.get("choice"),
                score=a.get("score"),
                noul=a.get("noul"),
                confidence=float(a.get("confidence", 0.0)),
                probabilities=a.get("probabilities", {}),
            )
            answer.items.append(item)
        
        answer.success = True
        
    except json.JSONDecodeError as e:
        answer.error = str(e)
        logger.warning(f"Jev answer parse failed: {e}")
    except Exception as e:
        answer.error = str(e)
        logger.warning(f"Jev answer parse failed: {e}")

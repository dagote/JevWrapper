"""Wire format types for Jev client."""

from enum import Enum
from dataclasses import dataclass, field
from typing import List, Dict, Optional


class QuestionType(Enum):
    """Types of questions supported by Jev."""
    CHOICE = "choice"
    SCORE = "score"
    NOUL = "noul"


@dataclass
class JevQuestionDef:
    """Definition of a single question."""
    name: str
    type: QuestionType
    instructions: str
    options: List[str] = field(default_factory=list)
    criteria: Dict[str, str] = field(default_factory=dict)


class JevQuestions:
    """Builder for constructing questions."""

    def __init__(self):
        self.items: List[JevQuestionDef] = []

    def add_choice(
        self, name: str, instructions: str, *options: str
    ) -> "JevQuestions":
        """Add a choice question with the given options."""
        self.items.append(
            JevQuestionDef(
                name=name,
                type=QuestionType.CHOICE,
                instructions=instructions,
                options=list(options),
            )
        )
        return self

    def describe(self, option: str, meaning: str) -> "JevQuestions":
        """Add description for an option in the most recently added question."""
        if self.items:
            self.items[-1].criteria[option] = meaning
        return self

    def add_score(
        self, name: str, instructions: str, *levels: str
    ) -> "JevQuestions":
        """Add a score question with the given levels."""
        self.items.append(
            JevQuestionDef(
                name=name,
                type=QuestionType.SCORE,
                instructions=instructions,
                options=list(levels),
            )
        )
        return self

    def add_noul(self, name: str, instructions: str) -> "JevQuestions":
        """Add a Noul (numeric, open-ended, 0-1) question."""
        self.items.append(
            JevQuestionDef(
                name=name,
                type=QuestionType.NOUL,
                instructions=instructions,
            )
        )
        return self


@dataclass
class JevAnswerItem:
    """A single answer from the model."""
    name: str
    type: QuestionType
    choice: Optional[str] = None  # for Choice
    score: Optional[float] = None  # for Score
    noul: Optional[float] = None  # for Noul (0-1)
    confidence: float = 0.0
    probabilities: Dict[str, float] = field(default_factory=dict)


@dataclass
class JevAnswer:
    """Complete response from the Jev endpoint."""
    group: str
    success: bool = False
    error: Optional[str] = None
    items: List[JevAnswerItem] = field(default_factory=list)
    raw_json: Optional[str] = None

    def get(self, name: str) -> Optional[JevAnswerItem]:
        """Get an answer item by name."""
        for item in self.items:
            if item.name == name:
                return item
        return None

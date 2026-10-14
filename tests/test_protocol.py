"""Tests for Jev protocol layer."""

import json
import pytest

from jev.protocol import build_request_json, build_state_text, parse_answer
from jev.types import JevAnswer, JevQuestions, QuestionType


class TestBuildStateText:
    """Tests for build_state_text function."""

    def test_empty_state(self):
        """Empty state produces empty string."""
        result = build_state_text({})
        assert result == ""

    def test_single_value(self):
        """Single value is formatted correctly."""
        result = build_state_text({"health": 10})
        assert result == "health is 10."

    def test_multiple_values(self):
        """Multiple values are formatted with periods."""
        result = build_state_text({"health": 10, "mana": 5})
        # Dict order is preserved in Python 3.7+
        assert "health is 10." in result
        assert "mana is 5." in result

    def test_float_values(self):
        """Float values use proper formatting."""
        result = build_state_text({"health": 10.5})
        assert "10.5" in result

    def test_invariant_culture(self):
        """Float formatting is locale-independent (always uses dot)."""
        result = build_state_text({"value": 0.5})
        assert "0.5" in result


class TestBuildRequestJson:
    """Tests for build_request_json function."""

    def test_simple_choice_question(self):
        """Build a simple choice question request."""
        questions = JevQuestions().add_choice(
            "action", "What should we do?", "Attack", "Flee", "Heal"
        )
        json_str = build_request_json("jev-3b", "Health is low.", questions)
        data = json.loads(json_str)

        assert data["model"] == "jev-3b"
        assert data["state"] == "Health is low."
        assert "action" in data["questions"]
        assert data["questions"]["action"]["type"] == "choice"
        assert set(data["questions"]["action"]["criteria"].keys()) == {
            "Attack",
            "Flee",
            "Heal",
        }

    def test_question_with_descriptions(self):
        """Descriptions are included in criteria."""
        questions = (
            JevQuestions()
            .add_choice("height", "Is the drone at target height?", "Low", "Ok", "High")
            .describe("Low", "Below target")
            .describe("Ok", "At target")
        )
        json_str = build_request_json("jev-latest", "State text.", questions)
        data = json.loads(json_str)

        criteria = data["questions"]["height"]["criteria"]
        assert criteria["Low"] == "Below target"
        assert criteria["Ok"] == "At target"
        assert criteria["High"] == "High"  # No description, uses option name

    def test_score_question(self):
        """Score questions are formatted correctly."""
        questions = JevQuestions().add_score(
            "difficulty", "Rate the difficulty", "Easy", "Medium", "Hard"
        )
        json_str = build_request_json("jev-latest", "State.", questions)
        data = json.loads(json_str)

        assert data["questions"]["difficulty"]["type"] == "score"
        assert set(data["questions"]["difficulty"]["criteria"].keys()) == {
            "Easy",
            "Medium",
            "Hard",
        }

    def test_noul_question(self):
        """Noul questions have no criteria."""
        questions = JevQuestions().add_noul("sentiment", "How do you feel?")
        json_str = build_request_json("jev-latest", "State.", questions)
        data = json.loads(json_str)

        assert data["questions"]["sentiment"]["type"] == "noul"
        assert "criteria" not in data["questions"]["sentiment"]

    def test_output_is_compact(self):
        """JSON output is compact (no extra whitespace)."""
        questions = JevQuestions().add_choice("act", "Do it?", "Yes", "No")
        json_str = build_request_json("jev-latest", "State.", questions)
        # Compact JSON should not have spaces after : or ,
        assert ": " not in json_str
        assert ", " not in json_str


class TestParseAnswer:
    """Tests for parse_answer function."""

    def test_parse_choice_response(self):
        """Parse a real choice response from the endpoint."""
        response_json = (
            '{"model":"jev-latest","answers":{"action":{"type":"choice",'
            '"choice":"Flee","probabilities":{"Attack":0.0013,"Flee":0.9987,'
            '"Heal":0},"confidence":0.9987}},"usage":{"input_tokens":19,'
            '"output_tokens":1},"ms":43,"authenticated":false}'
        )
        answer = JevAnswer(group="test")
        parse_answer(response_json, answer)

        assert answer.success is True
        assert len(answer.items) == 1

        item = answer.get("action")
        assert item is not None
        assert item.name == "action"
        assert item.type == QuestionType.CHOICE
        assert item.choice == "Flee"
        assert abs(item.confidence - 0.9987) < 1e-4
        assert item.probabilities["Attack"] == 0.0013
        assert item.probabilities["Flee"] == 0.9987

    def test_parse_no_answers_field(self):
        """Response without 'answers' field produces error."""
        response_json = '{"model":"jev-latest"}'
        answer = JevAnswer(group="test")
        parse_answer(response_json, answer)

        assert answer.success is False
        assert answer.error == "response has no 'answers'"

    def test_parse_invalid_json(self):
        """Invalid JSON produces error."""
        answer = JevAnswer(group="test")
        parse_answer("not json", answer)

        assert answer.success is False
        assert answer.error is not None

    def test_parse_multiple_answers(self):
        """Multiple answers are all parsed."""
        response_json = (
            '{"answers":{"q1":{"type":"choice","choice":"A","confidence":0.5,'
            '"probabilities":{}},"q2":{"type":"score","score":3.5,'
            '"confidence":0.9,"probabilities":{}}}}'
        )
        answer = JevAnswer(group="test")
        parse_answer(response_json, answer)

        assert answer.success is True
        assert len(answer.items) == 2
        assert answer.get("q1").choice == "A"
        assert answer.get("q2").score == 3.5

    def test_parse_noul_answer(self):
        """Noul answers are parsed correctly."""
        response_json = (
            '{"answers":{"feeling":{"type":"noul","noul":0.75,'
            '"confidence":0.8,"probabilities":{}}}}'
        )
        answer = JevAnswer(group="test")
        parse_answer(response_json, answer)

        assert answer.success is True
        item = answer.get("feeling")
        assert item.type == QuestionType.NOUL
        assert item.noul == 0.75

    def test_raw_json_stored(self):
        """Raw JSON is stored for debugging."""
        response_json = '{"answers":{"test":{"type":"choice","choice":"X","confidence":0}}}'
        answer = JevAnswer(group="test")
        parse_answer(response_json, answer)

        assert answer.raw_json == response_json

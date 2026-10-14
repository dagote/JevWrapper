# Jev Python Client

Python client for the System One / Jev decision bridge.

## Installation

```bash
pip install jev
```

## Usage

```python
from jev import JevClient, JevQuestions

# Create a client with your settings
client = JevClient()
client.settings.endpoint = "https://www.dagote.ai/api/jev/v1/systemone"
client.settings.api_key = "your-api-key"
client.settings.model = "jev-latest"

# Create a question
questions = (
    JevQuestions()
    .add_choice("height", "Is the drone below, at, or above its target height?", "Low", "Ok", "High")
    .describe("Low", "The drone is below its target height.")
    .describe("Ok", "The drone is at its target height, give or take half a meter.")
    .describe("High", "The drone is above its target height.")
)

# Ask a question and handle the response
def on_answer(answer):
    if not answer.success:
        print(f"Error: {answer.error}")
        return
    
    height_item = answer.get("height")
    print(f"Height: {height_item.choice} (confidence: {height_item.confidence:.2f})")
    print(f"Probabilities: {height_item.probabilities}")

# Option 1: Manually specify state as prose
client.question(
    "drone_01",
    questions,
    on_answer,
    state_text="The drone is 3 meters below its target height."
)

# Option 2: Use signals (stored in client.context) and let them be rendered
client.update("altitude", "drone_01", 124.5)
client.update("target_altitude", "drone_01", 127.5)
client.question("drone_01", questions, on_answer)
```

## Architecture

- **types.py**: Question/Answer data classes (QuestionType enum, builders)
- **settings.py**: Connection settings (endpoint, API key, model, timeout)
- **context.py**: Per-group signal storage (latest value only, no history)
- **protocol.py**: Request building and response parsing (no networking, testable)
- **client.py**: Main client with HTTP handling

## Testing

```bash
pip install -e ".[dev]"
pytest
```

## Wire Format

Request (POST to endpoint):
```json
{
  "model": "jev-latest",
  "state": "health is 10. mana is 5.",
  "questions": {
    "action": {
      "type": "choice",
      "instructions": "What should we do?",
      "criteria": {
        "Attack": "Attack the enemy",
        "Flee": "Run away",
        "Heal": "Restore health"
      }
    }
  }
}
```

Response:
```json
{
  "answers": {
    "action": {
      "type": "choice",
      "choice": "Flee",
      "confidence": 0.9987,
      "probabilities": {
        "Attack": 0.0013,
        "Flee": 0.9987,
        "Heal": 0
      }
    }
  }
}
```

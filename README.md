# JevWrapper Client Libraries

Multi-language client wrappers for the Jev decision bridge (TypeSafe AI System One Model).

## Packages

### Python

Install from PyPI:
```bash
pip install jevwrapper
```

[See python/README.md](python/README.md) for documentation and usage.

### C# / .NET

For **Unity**, add the package via git URL in `Packages/manifest.json`:
```json
{
  "dependencies": {
    "ai.dagote.jevwrapper": "https://github.com/dagote/JevWrapper.git?path=dotnet/Jev#v0.1.0"
  }
}
```

For **regular .NET apps**, install from NuGet:
```bash
dotnet add package ai.dagote.jevwrapper
```

## Repository Structure

```
JevWrapper/
  python/
    jevwrapper/       # Main package
      __init__.py
      types.py        # Data models
      protocol.py     # Request/response logic
      settings.py     # Configuration
      context.py      # Signal storage
      client.py       # Main client
    pyproject.toml    # Package metadata
    README.md
  
  dotnet/Jev/         # C# client wrapper library
    *.cs              # Source files
    Jev.csproj        # NuGet package metadata
    package.json      # Unity package metadata
    JevProtocolTests.cs
  
  examples/
    python/           # Python examples
    dotnet/           # C# examples
  
  tests/
    test_protocol.py  # Python tests (run against recorded responses)
  
  .github/workflows/
    publish.yml       # CI/CD: run tests, publish to PyPI and NuGet on version tags

  pyproject.toml      # Root Python config
  README.md           # This file
```

## Wire Format

Both Python and C# clients use the same JSON protocol:

**Request** (POST to Jev endpoint):
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

**Response**:
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

## HTTP Headers

- `Content-Type: application/json`
- `x-api-key: {your-api-key}` (if using API key authentication)
- **Do NOT send** `Authorization: Bearer {key}` (this header is ignored and will keep requests on guest path)

## Testing

Each language has contract tests that run against recorded responses:

**Python**:
```bash
cd python
pip install -e ".[dev]"
pytest
```

**C#**:
```bash
dotnet test dotnet/Jev/JevProtocolTests.cs
```

CI/CD runs these tests on every push and publishes to package repositories on version tags.

## Default Endpoint

`https://www.dagote.ai/api/jev/v1/systemone`

## Supported Question Types

- **choice**: Select from a list of options
- **score**: Rate on a scale (e.g., "Low", "Medium", "High")
- **noul**: Open-ended numeric value (0-1)

## Quick Start

### Python
```python
from jev import JevClient, JevQuestions

client = JevClient()
client.settings.api_key = "your-key"

questions = (
    JevQuestions()
    .add_choice("action", "What to do?", "Attack", "Flee", "Heal")
)

answer = client.question("group_01", questions)
if answer.success:
    print(answer.get("action").choice)
```

### C#
```csharp
using JevWrapper;

var client = new JevClient();
client.Settings.ApiKey = "your-key";

var questions = new JevQuestions()
    .AddChoice("action", "What to do?", "Attack", "Flee", "Heal");

var answer = await client.QuestionAsync("group_01", questions);
if (answer.Success)
    Console.WriteLine(answer.Get("action").Choice);
```

## Publishing

Tagged releases (v0.1.0, v0.2.0, etc.) trigger GitHub Actions to:
1. Run all tests
2. Publish Python package to PyPI
3. Publish C# package to NuGet
4. Keep git tags for Unity Package Manager

## License

MIT

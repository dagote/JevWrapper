# JevWrapper (Unity)

Unity package for the System One / Jev decision bridge. Package id: `com.dagote.jevwrapper`.

## Layout
```
Packages/com.dagote.jevwrapper/
  package.json
  Runtime/
    Jev.cs            static facade over a default client
    JevClient.cs      one connection: settings, signal context, requests
    JevSettings.cs    endpoint, key, model, timeout
    JevContext.cs     latest value per signal per group
    JevTypes.cs       questions and answers
    JevProtocol.cs    request building / response parsing (internal)
    JevWrapper.asmdef
  Tests/EditMode/     NUnit tests for the protocol layer
```

Depends on `com.unity.nuget.newtonsoft-json`.

## Usage

```csharp
using JevWrapper;

// Default client, shared by anything that uses the static Jev class
Jev.Endpoint = "https://www.dagote.ai/api/jev/v1/systemone";
Jev.ApiKey   = "your-key";
Jev.Model    = "jev-3b";

var q = new JevQuestions()
    .AddChoice("height", "Is the drone below, at, or above its target height?", "Low", "Ok", "High")
    .Describe("Low",  "The drone is below its target height.")
    .Describe("Ok",   "The drone is at its target height, give or take half a meter.")
    .Describe("High", "The drone is above its target height.");

// Describe the situation in words. The model reads text, not numbers.
Jev.Question("drone_01", q, answer =>
{
    if (!answer.Success) { Debug.LogWarning(answer.Error); return; }
    var height = answer.Get("height");          // Choice, Confidence, Probabilities
    Debug.Log($"{height.Choice} ({height.Confidence:0.00})");
}, "The drone is 3 meters below its target height.", owner: this);
```

Or push signals and let the wrapper render them as "name is value" sentences:

```csharp
Jev.Update("altitude", "drone_01", 124.5f);
Jev.Question("drone_01", q, OnAnswer);   // stateText omitted
```

### Independent clients

Anything that needs its own model, key or signal groups should own a client so it can't
affect other systems:

```csharp
var settings = Jev.Default.Settings.Clone();
settings.Model = "jev-3b";
var client = new JevClient(settings);
client.Question("drone_01", q, OnAnswer, stateText, owner: this);
```

## Behaviour
- Groups are mandatory. Context is per client.
- `Question` returns `false`, with no callback, if arguments are invalid or a request for that group is already in flight.
- `owner` is optional. If it is destroyed before the answer arrives, the request is aborted and the callback is skipped.
- Requests time out after `Settings.TimeoutSeconds` (default 30). Failures arrive as `answer.Success == false` with `answer.Error`.
- The callback runs on the main thread.
- The key is sent as `x-api-key` (and as a `Bearer` token).
- Statics are reset on every Play Mode entry, so it works with "Reload Domain" off.

## Writing questions for the small models
Learned against `jev-latest` and `jev-3b`:
- Classify, don't command. Ask "is the drone Low, Ok or High?" and map the answer to a move in your own code. "Which way should it move?" was answered with the same option regardless of state.
- Describe state in sentences that match the option wording ("is 3 meters below its target height" next to an option meaning "below its target height").
- Use short, common words for options. Multi-token words such as `Flee` and `Descend` were chosen far too often.
- Treat low-confidence answers as "no change". See `minConfidence` in the drone demo.

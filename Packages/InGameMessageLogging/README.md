# In Game Message Logging

Shows log messages inside a running game as a capped, self-expiring on-screen list — so a build on a
device can be diagnosed without attaching an editor or an external debugger.

Presentation is entirely `UnityEvent`-driven. The package supplies the message bookkeeping (capacity,
lifetime, eviction); how a message looks, fades in and fades out is authored on the prefab.

## What it provides

| Type | Namespace | Purpose |
|---|---|---|
| `IInGameMessageLogger` | `Arman.Foundation.InGameMessageLogging` | The contract — a single `Log(string message)`. |
| `UnityInGameMessageLogger` | `Arman.Presentation.InGameMessageLogging` | `MonoBehaviour` implementation that instantiates one message per log call. |
| `LogMessage` | `Arman.Presentation.InGameMessageLogging` | The message view, driven by `UnityEvent`s. |

`UnityInGameMessageLogger` is configured in the Inspector:

| Field | Meaning |
|---|---|
| `loggerMessagePrefab` | The `LogMessage` prefab instantiated per message. |
| `messageContainer` | The parent `GameObject` messages are added under. |
| `capacity` | Maximum messages on screen; the oldest is cleared when exceeded. |
| `logLifeTime` | Seconds a message lives before clearing itself. |

`LogMessage` exposes four events the prefab wires up: `setTextAction` (`StringUnityEvent`, receives
the message), `fadeInAction`, `startTimeAction` (`FloatUnityEvent`, receives the lifetime) and
`fadeOutAction`.

## Usage

Log from anywhere that can reach the component, behind the interface:

```csharp
using Arman.Foundation.InGameMessageLogging;

public class Enemy : MonoBehaviour
{
    private IInGameMessageLogger _logger;

    private void OnDeath() => _logger.Log($"{name} died at {transform.position}");
}
```

```csharp
using Arman.Presentation.InGameMessageLogging;

// The logger is a MonoBehaviour in the scene — reference it, don't construct it.
IInGameMessageLogger logger = inGameMessageLoggerComponent;
logger.Log("Level loaded");
```

### Setting up the prefab

1. Build a `LogMessage` prefab with whatever visuals you want (a `Text`, a `CanvasGroup`, an
   `Animator`).
2. On the `LogMessage` component, wire the events:
   - `setTextAction` → the text component's `text` setter,
   - `fadeInAction` → your fade-in animation,
   - `startTimeAction` → a `DelayHandler.StartTimer(float)` from `Unity Utilities`,
   - `fadeOutAction` → your fade-out animation.
3. Point the `DelayHandler`'s timeout at `LogMessage.FadeOut`, and have the end of the fade-out call
   `LogMessage.ClearSelf`.
4. Assign the prefab and a container to `UnityInGameMessageLogger`, and set `capacity` and
   `logLifeTime`.

A worked example ships as the *In Game Message Logging Example* sample, which wires an `InputField`
to the logger — import it from the package's Samples tab in the Package Manager.

## Things to know

- **Nothing hooks Unity's console.** This does not subscribe to `Application.logMessageReceived`;
  you call `Log` explicitly. Route `Debug.Log` into it yourself if you want both.
- **`ClearSelf` destroys the message GameObject**, so every message costs an `Instantiate`. Keep
  `capacity` small and don't log per frame.
- **Eviction is by insertion order.** At `capacity`, the oldest message is cleared to make room —
  which invokes its clear callback and destroys it immediately, without fading out.
- **Presentation is entirely your prefab's job.** If `fadeOutAction` is never wired to something that
  calls `ClearSelf`, messages accumulate until `capacity` evicts them.

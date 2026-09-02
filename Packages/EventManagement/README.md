# Event Management

A broadcast event bus for decoupling gameplay systems. A sender propagates an `IGameEvent`; every
registered listener receives it and decides for itself whether it cares.

Pure C# with no `UnityEngine` dependency, so the bus and its listeners are testable as a plain
library.

## What it provides

Everything lives in the `Arman.EventManagement` namespace.

| Type | Purpose |
|---|---|
| `IGameEvent` | Marker interface for an event. Carry whatever payload you like. |
| `IEventListener` | `OnEvent(IGameEvent evt, object sender)`. |
| `IEventManager` | `Propagate`, `Register`, `UnRegister`, `Has`, `Clear`. |
| `BasicEventManager` | The implementation. |

## Usage

An event is a plain class:

```csharp
using Arman.EventManagement;

public class PlayerDied : IGameEvent
{
    public readonly int Score;
    public PlayerDied(int score) => Score = score;
}
```

A listener receives everything and filters by type:

```csharp
public class ScoreBoard : IEventListener
{
    public void OnEvent(IGameEvent evt, object sender)
    {
        if (evt is PlayerDied died)
            Submit(died.Score);
    }
}
```

Wire it up and broadcast:

```csharp
var events = new BasicEventManager();

var scoreBoard = new ScoreBoard();
events.Register(scoreBoard);

events.Propagate(new PlayerDied(1200), this);

events.UnRegister(scoreBoard);
```

`Clear()` drops every listener at once — useful when tearing down a scene or resetting between tests.

## Things to know

- **Namespace simplification.** The runtime namespace is now `Arman.EventManagement`; the former `Arman.Foundation.EventManagement` namespace is gone. Update any `using` directives (and test namespaces, now `Arman.EventManagement.Tests`) to match.
- **This is a broadcast bus, not a subscription-by-type bus.** Every listener is called for every
  event; type filtering happens inside `OnEvent`. That keeps the manager trivial, and costs a virtual
  call per listener per event — fine for gameplay events, wrong for per-frame traffic.
- **Registration is deduplicated.** Registering the same listener twice is a no-op, so it will never
  be called twice for one event.
- **Propagation iterates a snapshot**, so a listener may safely register or unregister — including
  itself — while handling an event. A listener added during propagation does not receive the event
  currently being dispatched.
- **Dispatch is synchronous and single-threaded.** `Propagate` returns only after every listener has
  run, and the manager does no locking — call it from the main thread.
- **Listeners are held by strong reference.** A listener that is not unregistered stays alive; call
  `UnRegister` in your teardown, or `Clear()` on scene change.
- **Exceptions are not contained.** A listener that throws aborts the propagation, and the remaining
  listeners never see the event.

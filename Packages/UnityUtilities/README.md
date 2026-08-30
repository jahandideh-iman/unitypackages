# Unity Utilities

Small Unity helpers shared by the other Arman packages. Each is a self-contained `MonoBehaviour` or
serializable type that lets designers wire behaviour in the Inspector through `UnityEvent`s, rather
than a framework you buy into.

`In Game Message Logging` depends on this package for its typed `UnityEvent` variants.

## What it provides

Everything lives in the `Arman.Utilty.Unity` namespace.

| Type | Purpose |
|---|---|
| `DelayHandler` | A coroutine timer. Raises `timeoutEvent` after `duration` seconds; `StartTimer()`, `StartTimer(float)`, `StopTimer()`, optional `autoStart`. |
| `UnityAnimationPlayer` | Wrapper over the legacy `Animation` component — `Play()`, `Play(name)`, `Play(clip)`, `RemoveClip(name)`, `Stop()`. |
| `UnityAnimatorEventHandler` | Receives `OnAnimationEvent(string)` from an animation clip and invokes the matching named `UnityEvent`. |
| `UnityEventDelegator` | Holds a list of id/`UnityEvent` pairs; `Delegate(id)` invokes one, `DelegateAll()` invokes all. |
| `BooleanUnityEvent`, `FloatUnityEvent`, `IntUnityEvent`, `StringUnityEvent` | Serializable `UnityEvent<T>` subclasses. |

## Usage

### Typed UnityEvents

Unity does not serialize an open generic `UnityEvent<T>`, so a concrete subclass is needed for each
argument type. These four cover the common primitives:

```csharp
using Arman.Utilty.Unity;
using UnityEngine;

public class HealthDisplay : MonoBehaviour
{
    // Shows up in the Inspector with an int argument slot.
    [SerializeField] private IntUnityEvent _onHealthChanged = default;

    public void SetHealth(int value) => _onHealthChanged.Invoke(value);
}
```

### DelayHandler

A timer you can drive from the Inspector without writing a coroutine:

```csharp
// duration and timeoutEvent are set in the Inspector.
delayHandler.StartTimer();        // uses the serialized duration
delayHandler.StartTimer(2.5f);    // overrides it for this run
delayHandler.StopTimer();
```

Tick `autoStart` to have it begin in `Start()`.

### UnityAnimatorEventHandler

Add the component, populate `animationEvents` with name/event pairs, then have an Animation Event on
the clip call `OnAnimationEvent` with the matching name. Each name fires its own `UnityEvent`, so one
clip can drive several unrelated reactions.

### UnityEventDelegator

Groups `UnityEvent`s behind string ids, so one component can fan a call out to several listeners:

```csharp
eventDelegator.Delegate("LevelComplete");
eventDelegator.DelegateAll();
```

### UnityAnimationPlayer

Requires an `Animation` component on the same GameObject (`[RequireComponent]`):

```csharp
animationPlayer.Play("Idle");
animationPlayer.Play(introClip);      // adds the clip, then plays it by its own name
animationPlayer.Stop();
```

## Things to know

- **The namespace is `Arman.Utilty.Unity`** — note the spelling. It is kept as-is because renaming it
  would break every consumer's `using`.
- **`DelayHandler` calls `StopAllCoroutines()`** when a timer starts or stops. If you run other
  coroutines on the same GameObject, they will be cancelled too — give the handler its own object.
- **`UnityAnimationPlayer` wraps the legacy `Animation` component**, not `Animator`. It is for
  older clip-based setups.
- **`UnityAnimatorEventHandler` matches by exact string.** An unmatched event name is silently
  ignored, and duplicate names all fire.

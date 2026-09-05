# Update Management

Drives game logic from one update loop instead of hundreds of `MonoBehaviour.Update` callbacks.
Objects implement `IUpdatable` and register against a channel; pausing a channel freezes everything on
it, and channels nest — pausing a parent pauses its children.

That is what makes a pause menu simple: put gameplay on one channel and UI on another, pause the
gameplay channel, and every enemy, timer and animation on it stops without any of them knowing a menu
exists.

## What it provides

Namespace `Arman.UpdateManagement.Foundation`:

| Type | Purpose |
|---|---|
| `IUpdatable` | `UpdateTime(float dt)` — named to avoid clashing with Unity's `Update`. |
| `IUpdateManager` | Channel registration, updatable registration, pause/resume, and `ChannelStateChangedEvent`. |
| `BasicUpdateManager` | The implementation, plus `AdvanceTime(float)`. |

Namespace `Arman.UpdateManagement.Foundation.Unity`:

| Type | Purpose |
|---|---|
| `UnityUpdateManager` | `MonoBehaviour` that calls `AdvanceTime(Time.deltaTime)` every frame. |

Channels come from `Package Basics` (`IChannel`, `NamedChannel`, `IDedChannel`).

## Usage

Implement `IUpdatable` rather than Unity's `Update`:

```csharp
using Arman.UpdateManagement.Foundation;

public class Enemy : MonoBehaviour, IUpdatable
{
    public void UpdateTime(float dt) => transform.position += _velocity * dt;
}
```

Register it on a channel. Drop a `UnityUpdateManager` in the scene and it ticks for you:

```csharp
using Arman.Utility.Core;

IChannel gameplay = new NamedChannel("gameplay");
IChannel ui       = new NamedChannel("ui");

updateManager.RegisterUpdatable(enemy, gameplay);
updateManager.RegisterUpdatable(hudClock, ui);
```

Pause a whole slice of the game:

```csharp
updateManager.Pause(gameplay);   // enemies freeze, the HUD keeps ticking
updateManager.Resume(gameplay);

bool frozen = updateManager.IsChannelGloballyPaused(gameplay);
```

Nest channels so one pause covers a group:

```csharp
IChannel world = new NamedChannel("world");

updateManager.RegisterChannel(world);
updateManager.RegisterChannelToParent(gameplay, world);
updateManager.RegisterChannelToParent(particles, world);

updateManager.Pause(world);      // pauses gameplay and particles too
```

Unregister when an object dies, or the manager keeps calling it:

```csharp
updateManager.UnRegisterUpdatable(enemy);
```

Driving the manager yourself — in a test, a server build, or a fixed-step loop — is just
`AdvanceTime`:

```csharp
var manager = new BasicUpdateManager();
manager.RegisterUpdatable(enemy, gameplay);

manager.AdvanceTime(0.016f);     // one deterministic step
```

## Things to know

- **`SetChannelTimeScale` throws `NotImplementedException`.** Per-channel time scaling is not
  supported yet — the interaction with parent channel scales is unresolved. Every channel runs at 1×.
  Do not call it.
- **`AdvanceTime` is on `BasicUpdateManager`, not on `IUpdateManager`.** Code holding the interface can
  register and pause but cannot tick; that is deliberate, so only the owner of the loop advances time.
- **Channel parent cycles are not detected** and will hang the pause check. Keep the channel graph a
  tree.
- **`IsChannelGloballyPaused` throws for an unregistered channel** — it looks the channel up directly.
  `Pause` and `Resume` silently do nothing instead. Call `RegisterChannel` first.
- **`RegisterUpdatable` creates the channel if it is new**, so an explicit `RegisterChannel` is only
  needed for a channel you want to configure or pause before anything registers on it.
- **Registration is not deduplicated.** Registering the same object twice makes it tick twice per
  frame; `UnRegisterUpdatable` then removes every copy at once, from whichever channel holds it.
- **Updatables are iterated over a per-frame snapshot**, so registering or unregistering from inside
  `UpdateTime` is safe. Objects are ticked in reverse registration order.
- **`ChannelStateChangedEvent` fires on every `Pause`/`Resume`** of a registered channel, reporting
  that channel's own flag — not the inherited state of its children.
- **An updatable that throws is dropped.** If `UpdateTime` throws, the manager unregisters that
  updatable and keeps ticking the rest of the channel. The alternative is worse than it looks: the
  offender stays registered, so it throws again on the very next frame and its channel never
  advances past it. The usual cause is a destroyed Unity object that was never unregistered.

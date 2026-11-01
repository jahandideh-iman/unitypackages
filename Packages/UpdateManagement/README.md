# Update Management

Drives game logic from one update loop instead of hundreds of `MonoBehaviour.Update` callbacks.
Objects implement `IUpdatable` and register against a channel; pausing a channel freezes everything on
it, and channels nest — pausing a parent pauses its children.

That is what makes a pause menu simple: put gameplay on one channel and UI on another, pause the
gameplay channel, and every enemy, timer and animation on it stops without any of them knowing a menu
exists.

## What it provides

Namespace `Arman.UpdateManagement`:

| Type                 | Purpose                                                                                     |
| -------------------- | ------------------------------------------------------------------------------------------- |
| `IUpdatable`         | `UpdateTime(float dt)` — named to avoid clashing with Unity's `Update`.                     |
| `IUpdateManager`     | Channel registration, updatable registration, pause/resume, and `ChannelStateChangedEvent`. |
| `UpdateManager`      | The implementation, plus `AdvanceTime(float)`.                                              |
| `UnityUpdateManager` | `MonoBehaviour` that calls `AdvanceTime(Time.deltaTime)` every frame.                       |

Channels come from `Package Basics` (`Arman.PackageBasics`: `IChannel`, `NamedChannel`, `IDedChannel`).

## Usage

Implement `IUpdatable` rather than Unity's `Update`:

```csharp
using Arman.UpdateManagement;

public class Enemy : MonoBehaviour, IUpdatable
{
    public void UpdateTime(float dt) => transform.position += _velocity * dt;
}
```

Register it on a channel. Drop a `UnityUpdateManager` in the scene and it ticks for you:

```csharp
using Arman.PackageBasics;

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
var manager = new UpdateManager();
manager.RegisterUpdatable(enemy, gameplay);

manager.AdvanceTime(0.016f);     // one deterministic step
```

## Things to know

- **`SetChannelTimeScale` throws `NotImplementedException`.** Per-channel time scaling is not
  supported yet — the interaction with parent channel scales is unresolved. Every channel runs at 1×.
  Do not call it.
- **`AdvanceTime` is on `UpdateManager`, not on `IUpdateManager`.** Code holding the interface can
  register and pause but cannot tick; that is deliberate, so only the owner of the loop advances time.
- **Channel parent cycles are not detected** and will hang the pause check. Keep the channel graph a
  tree.
- **`IsChannelGloballyPaused` throws for an unregistered channel** — it looks the channel up directly.
  `Pause` and `Resume` silently do nothing instead. Call `RegisterChannel` first.
- **`RegisterUpdatable` creates the channel if it is new**, so an explicit `RegisterChannel` is only
  needed for a channel you want to configure or pause before anything registers on it.
- **Registration is not deduplicated.** Registering the same object twice makes it tick twice per
  frame. `UnRegisterUpdatable` removes every copy from the first channel that holds the object;
  a copy registered on another channel stays. Unregister each channel's copy separately.
- **Updatables are iterated over a per-frame snapshot of their channel**, so registering or
  unregistering updatables on an existing channel from inside `UpdateTime` is safe. Objects are
  ticked in reverse registration order.
- **Registering a new channel from inside `UpdateTime` throws `InvalidOperationException`.**
  `AdvanceTime` is enumerating the manager's channel table, so create every channel before the tick
  that first uses it.
- **`RegisterChannelToParent` throws for an unregistered parent.** Call `RegisterChannel(parent)`
  or register an updatable on the parent first.
- **`ChannelStateChangedEvent` fires on every `Pause`/`Resume`** of a registered channel, reporting
  that channel's own flag — not the inherited state of its children.
- **An updatable that throws stays registered.** If `UpdateTime` throws, the manager logs the
  exception with `Debug.LogException` and keeps ticking the rest of the channel and every channel
  after it. The offender is ticked again on every later frame, so it logs on every frame until you
  unregister it. The usual cause is a destroyed Unity object that was never unregistered.

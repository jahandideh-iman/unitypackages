# Development Console

An in-game cheat and debug menu. Mark a static method with `[DevOption]` and it appears as a button in
an on-screen panel — no UI wiring, no registration call, no per-command boilerplate. Methods that take
arguments get a typed input prompt; methods with a `[ShortCut]` fire on a key combination.

The point is that adding a cheat costs one attribute, so the console stays useful instead of rotting.

## What it provides

Everything lives in the `Arman.DevelopmentConsole` namespace.

| Type | Purpose |
|---|---|
| `DevelopmentOptionsDefinition` | Abstract base. Subclass it to have your options discovered. |
| `DevOptionAttribute` | `[DevOption(group, commandName)]` on a static method. |
| `ShortCutAttribute` | `[ShortCut(KeyCode..., KeyCode...)]` — a key combination for the same method. |
| `DevelopmentConsolePanel` | The `MonoBehaviour` that scans, builds the UI and polls shortcuts. |
| `DevelopmentGroup`, `DevelopmentCommand` | The group and button views, instantiated from prefabs. |
| `CommandInfo` | A discovered command: name, method, shortcut keys. |
| `CommandInputPrompt` | The argument prompt for commands that take parameters. |
| `ReflectionUtilities` | Assembly-wide type lookup helpers. |
| `FPSProfiler`, `ButtonDragabler` | An on-screen frame-rate readout, and a draggable button. |

## Usage

Declare options by subclassing `DevelopmentOptionsDefinition`. The methods must be `static`:

```csharp
using Arman.DevelopmentConsole;
using UnityEngine;

public class CheatOptions : DevelopmentOptionsDefinition
{
    [DevOption("Player", "Heal")]
    public static void Heal() => Game.Player.Health = 100;

    [DevOption("Player", "God Mode")]
    [ShortCut(KeyCode.LeftShift, KeyCode.G)]
    public static void ToggleGodMode() => Game.Player.Invincible = !Game.Player.Invincible;

    // Takes a parameter, so clicking it opens an input prompt.
    [DevOption("Levels", "Load Level")]
    public static void LoadLevel(int index) => SceneManager.LoadScene(index);
}
```

Then put a `DevelopmentConsolePanel` prefab in your scene. On `Awake` it scans the loaded assemblies
for `DevelopmentOptionsDefinition` subclasses and builds one group per `group` string, with a button
per command. Nothing else registers anything.

The panel also exposes `onErrorDetected`, `onToolsPanelOpened` and `onToolsPanelClosed` as
`UnityEvent`s. `onErrorDetected` fires whenever Unity logs an error or an exception. It ships with no
listener attached, so wire it to whatever you want — flashing the dev button red is a cheap way to
notice that something has gone wrong off-screen.

Two worked definitions ship as the *Development Console Example* sample — import it from the
package's Samples tab in the Package Manager.

## Things to know

- **Options must be `static`.** A non-static method carrying `[DevOption]` or `[ShortCut]` is skipped
  with an error in the log. There is no instance to invoke against — reach your objects through a
  singleton or a lookup inside the method.
- **Discovery walks every loaded assembly** once, in `Awake`. That is a real cost at startup, so keep
  the panel out of release builds — gate the prefab behind `Debug.isDebugBuild` or a define.
- **Command names must be unique within one definition class.** Two `[DevOption]`s sharing a name in
  the same class throw when the panel initialises. Across classes it is fine.
- **Groups are built per definition class.** Two classes using the same group string produce two
  groups with the same heading rather than one merged group.
- **Argument prompts support `int`, `float` and `string` only.** Any other parameter type logs an
  error and passes `null`. Parsing is unguarded, so non-numeric text in a number field throws.
- **Shortcuts are polled in `Update` via the legacy `Input` class.** They fire while the panel object
  is active, whether or not the panel is visible, and are not consumed — a combination that overlaps
  your gameplay input will trigger both. This does not work with the new Input System package.
- **`DevOptionAttribute` derives from Unity's `[Preserve]`**, so annotated methods survive managed
  code stripping in IL2CPP builds.

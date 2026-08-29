# Development Console

An in-game development console that groups commands and options into panels. Options are authored with the `[DevOption]` attribute, registered at runtime via reflection, grouped, and driven by key-combo (shortcut) input. Includes an optional FPS profiler and a bundled log viewer.

## What it provides

- `DevelopmentConsolePanel`, `DevelopmentGroup`, `DevelopmentOptionsDefinition` — the panel/group/definition model.
- `DevelopmentCommand` / `CommandInfo` / `CommandInputPrompt` — command execution and input.
- `DevOptionAttribute`, `ShortCutAttribute` / `ShortCutInfo` — editor-time authoring and keyboard combos.
- `DevelopmentConsoleExtensions` (e.g. `RegisterDevOptions`) — wire options from a provider.
- `FPSProfiler` and a bundled Unity log viewer.

## Usage

```csharp
using Arman.DevelopmentConsole;
using Arman.DevelopmentConsole.Attributes;

[DevOption("Reload", "Reload the current scene", CommandScope.Current)]
public void ReloadScene() => UnityEngine.SceneManagement.SceneManager.LoadScene(0);

// At startup: register options and show the console.
DevelopmentConsolePanel panel = DevelopmentConsolePanel.GetOrCreate();
DevelopmentGroup tools = new DevelopmentGroup("Tools");
DevelopmentConsoleExtensions.RegisterDevOptions(tools, devOptionsProvider);
panel.AddGroup(tools);
panel.Show();
```

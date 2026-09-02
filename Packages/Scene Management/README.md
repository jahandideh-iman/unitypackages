# Scene Management

Two small pieces: an injectable wrapper around Unity's scene loading, and a base class that gives each
scene a single, reliably-first entry point.

The wrapper exists so callers depend on a type you can substitute in a test instead of calling the
static `UnityEngine.SceneManagement.SceneManager` directly. The initialiser exists so scene setup does
not race the `Awake` order of everything else in the scene.

## What it provides

Everything lives in the `Arman.SceneManagement` namespace.

| Type | Purpose |
|---|---|
| `SceneManager` | `Open(string sceneName)` — loads a scene. |
| `SceneInitilizer` | Abstract `MonoBehaviour`; override `Init()` for per-scene bootstrap. |

## Usage

Take a `SceneManager` as a dependency rather than calling Unity's static API:

```csharp
using Arman.SceneManagement;

public class LevelFlow
{
    private readonly SceneManager _scenes;

    public LevelFlow(SceneManager scenes) => _scenes = scenes;

    public void GoToMenu() => _scenes.Open("MainMenu");
}
```

Give each scene one bootstrap component:

```csharp
using Arman.SceneManagement;

public class LevelInitializer : SceneInitilizer
{
    protected override void Init()
    {
        // Wire up the scene here. Runs before other components' Awake.
    }
}
```

## Things to know

- **`Init()` runs from `Awake`, at execution order -100.** `[DefaultExecutionOrder(-100)]` puts it
  ahead of ordinary components, so anything the scene needs registered is ready before their `Awake`.
  Components with a more negative order of their own still run first.
- **The namespace is `Arman.SceneManagement`** — it was renamed from `Arman.SceneMangement` (which
  carried a "Mangement" typo), so any out-of-tree `using` referring to the old name needs updating.
  The class name `SceneInitilizer` keeps its separate misspelling for compatibility.
- **`SceneManager` shadows Unity's own `SceneManager`.** In a file that has both `using` directives you
  will need to qualify one of them.
- **`Open` is a single-scene, synchronous `LoadScene`.** There is no additive load, no async load and
  no progress reporting; call Unity's API directly when you need those.

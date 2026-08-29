# Scene Management

A thin helper for Unity scenes. `SceneManager` wraps scene loading, and the abstract `SceneInitilizer : MonoBehaviour` runs an `Init()` override in `Awake` (with a negative default execution order) so each scene can initialise itself.

## What it provides

- `SceneManager` — `Open(sceneName)` to load a scene via `SceneManager.LoadScene`.
- `SceneInitilizer` — abstract MonoBehaviour; implement `Init()` for per-scene bootstrap.

## Usage

```csharp
using Arman.SceneMangement;

// Load a scene directly.
var sceneManager = new SceneManager();
sceneManager.Open("Level1");

// Or drive per-scene init from a component:
public class LevelInitializer : SceneInitilizer
{
    protected override void Init()
    {
        // Runs in Awake of the initializing scene.
    }
}
```

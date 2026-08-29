# Update Management

Drives `IUpdatable` objects through an `IUpdateSystem`. Register objects with a `BasicUpdateManager` (or the delayed variant) at a given update order, and a `UnityUpdateManager` adapts Unity's `MonoBehaviour` update callbacks (`Update`, `LateUpdate`, `FixedUpdate`) to the manager.

## What it provides

- `IUpdateManager` / `IUpdateSystem` — the update contracts.
- `BasicUpdateManager` — register/unregister `IUpdatable`s by update order.
- `DelayedUpdateSystem` — `IUpdateSystem`, `IUpdateManager` and `IUpdatable` support for delayed updates.
- `UnityUpdateManager` — bridges Unity's `MonoBehaviour` callbacks to an `IUpdateSystem`.

## Usage

```csharp
using Arman.UpdateManagement;
using Arman.UpdateManagement.Unity;

IUpdateSystem system = new UnityUpdateManager();
IUpdateManager manager = new BasicUpdateManager(system);

manager.RegisterUpdateObject(myUpdatable, 0);
```

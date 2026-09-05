# Object Pooling

Reuses objects instead of allocating and destroying them. A pool hands out an object with
`Acquire()` and takes it back with `Release()`, notifying the object through `IPoolable` on each
transition. Unity implementations pool `Component` prefabs under a container transform.

## What it provides

Namespace `Arman.ObjectPooling`:

| Type | Purpose |
|---|---|
| `IPoolable` | `OnAcquired()` and `OnReleased()` — the pooled object's lifecycle hooks. |
| `IObjectPool<T>` | `Acquire()`, `Release(obj)`, `Reserve(count)`, `Size()`. |
| `ObjectPool<T>` | Abstract pool; subclasses supply `CreateObject`, `ActivateObject`, `DeactivateObject`. |
| `UnityComponentObjectPool<T>` | `ObjectPool<T>` for `Component` prefabs; `SetComponentPrefab`, `SetPoolingContainer`. |
| `MonobehaviorObjectPool<T>` | `MonoBehaviour` front-end over a `UnityComponentObjectPool<T>`. |
| `ScriptableObjectPool<T>` | `ScriptableObject` front-end over the same, with `Setup(Transform)`. |

## Usage

Pooling a Unity component. The pooled type must implement `IPoolable`:

```csharp
using Arman.ObjectPooling;
using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    public void OnAcquired() => gameObject.SetActive(true);
    public void OnReleased() => gameObject.SetActive(false);
}
```

```csharp
using Arman.ObjectPooling;

var pool = new UnityComponentObjectPool<Bullet>();
pool.SetComponentPrefab(bulletPrefab);
pool.SetPoolingContainer(poolRoot);

pool.Reserve(50);          // pre-instantiate, so the first shots don't allocate

Bullet bullet = pool.Acquire();
// ... later
pool.Release(bullet);

int idle = pool.Size();    // objects sitting in the pool, not objects in use
```

Writing a pool for a plain C# type — subclass `ObjectPool<T>`:

```csharp
using Arman.ObjectPooling;

public class ProjectilePool : ObjectPool<Projectile>
{
    protected override Projectile CreateObject() => new Projectile();
    protected override void ActivateObject(Projectile obj) => obj.Enabled = true;
    protected override void DeactivateObject(Projectile obj) => obj.Enabled = false;
}
```

`MonobehaviorObjectPool<T>` and `ScriptableObjectPool<T>` wrap the component pool when you would
rather configure it as a scene component or as an asset:

```csharp
// MonoBehaviour variant: prefab, container and initial reserve are Inspector fields.
// Tick `autoSetup` to have Awake() call Setup(), or call it yourself.
bulletPool.Setup();
Bullet bullet = bulletPool.Acquire();

// ScriptableObject variant needs the container passed in.
bulletPoolAsset.Setup(poolRoot);
```

## Things to know

- **The API is `Acquire` / `Release`**, not get/return.
- **`Size()` counts idle objects, not live ones.** It is the depth of the internal stack, so it drops
  as objects are acquired and rises as they are released.
- **The pool grows on demand.** `Acquire` on an empty pool calls `CreateObject` rather than blocking
  or failing, so there is no maximum size.
- **`T` must implement `IPoolable`.** Both hooks are called by the pool — `OnAcquired` on handout,
  `OnReleased` on return — and are where activation state belongs.
- **`Reserve(count)` pre-warms by creating and immediately releasing**, so `OnReleased` and
  `DeactivateObject` run on every reserved object. Call it during loading, not mid-frame.
- **`Release` is not validated.** Releasing an object the pool never handed out, or releasing the
  same object twice, is not detected.
- **`ScriptableObjectPool<T>` is an asset and outlives play mode.** Its `Setup(Transform)` must be
  called again with a live container each time a scene loads.
- **Flat namespace.** The runtime lives in `Arman.ObjectPooling` (formerly
  `Arman.ObjectPooling.Core` and `Arman.ObjectPooling.Unity`); the scripts are flat under `Runtime/Scripts`.

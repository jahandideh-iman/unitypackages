# Object Pooling

A type-safe object-pooling framework. `BasicObjectPool<T>` implements `IObjectPool<T>` (pre-warm, `Get`, `Recycle`, `Count`) with custom create/on-pool/on-unpool actions, and Unity implementations specialise it for `MonoBehaviour`s, `ScriptableObject`s, and arbitrary `Component`s.

## What it provides

- `IObjectPool<T>` / `IPoolable` — the pooling contract and an optional marker interface.
- `BasicObjectPool<T>` — the generic pool.
- `MonobehaviorObjectPool`, `ScriptableObjectPool`, `UnityComponentObjectPool` — Unity specialisations.
- `ObjectPoolExtensions` (e.g. `GetPool`).

## Usage

```csharp
using Arman.ObjectPooling;

IObjectPool<MonoBehaviour> pool = new MonobehaviorObjectPool("PooledObject", null,
    () => new MyPooledBehaviour(),
    behaviour => behaviour.OnPooled(),
    behaviour => behaviour.OnUnPooled());

MonoBehaviour obj = pool.Get();
pool.Recycle(obj);
```

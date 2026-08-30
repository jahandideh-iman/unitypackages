# Component System

Composition over inheritance for plain C# game objects. An entity is a bag of components retrieved by
type — `entity.GetComponent<Health>()` — with no Unity dependency, so entities can be built and tested
outside the editor.

This is not an ECS. There is no world, no system scheduler and no archetype storage: it is the
composition half only, small enough to drop into an existing design.

## What it provides

Everything lives in the `Arman.Foundation.ComponentSystem.Core` namespace.

| Type | Purpose |
|---|---|
| `IComponent` | Marker interface. Any component type implements it. |
| `IEntity` | `AddComponent`, `GetComponent<T>()`, `AllComponents()`. |
| `BasicEntity` | The implementation, plus `AddComponents(params)`, `GetComponentFromEnd<T>()` and `GetComponent<T>(int)`. |
| `ISpecializedEntity<T>` / `BasicSpecializedEntity<T>` | An entity constrained to one component family, with a typed `List<T> AllComponents()`. |
| `ICache` | `TryCache(IComponent)` — a hook for caching frequently-read components. |
| `CacheableBasicEntity<T>` | `BasicEntity` that offers every added component to an `ICache`. |

## Usage

Components are ordinary classes:

```csharp
using Arman.Foundation.ComponentSystem.Core;

public class Health : IComponent
{
    public int Current;
}
```

```csharp
var entity = new BasicEntity();

entity.AddComponent(new Health { Current = 100 });
entity.AddComponents(new Position(), new Velocity());

Health health = entity.GetComponent<Health>();

foreach (IComponent component in entity.AllComponents())
    Debug.Log(component);
```

### Caching hot components

Repeated `GetComponent<T>()` calls are a linear scan. When a few components are read every frame,
give the entity a cache that grabs them once as they are added:

```csharp
public class MovementCache : ICache
{
    public Position Position;
    public Velocity Velocity;

    public void TryCache(IComponent component)
    {
        if (component is Position position) Position = position;
        if (component is Velocity velocity) Velocity = velocity;
    }
}

var entity = new CacheableBasicEntity<MovementCache>(new MovementCache());
entity.AddComponent(new Position());

// Direct field access on the hot path, no scan.
entity.Cache().Position.Set(x, y);
```

### Constraining an entity to one family

```csharp
var abilities = new BasicSpecializedEntity<IAbility>();
abilities.AddComponent(new Dash());

List<IAbility> all = abilities.AllComponents();
Dash dash = abilities.GetComponent<Dash>();
```

## Things to know

- **`GetComponent<T>()` returns the first assignable match** by linear scan, and `default(T)` — `null`
  for a class — when there is none. It does not throw. `GetComponentFromEnd<T>()` scans backwards and
  returns the last, which is how you pick the most recently added of a duplicated type.
- **Duplicates are allowed.** `AddComponent` never checks for an existing component of the same type.
- **`AllComponents()` returns the internal array**, rebuilt on every add. Adding in a loop is O(n²) —
  use `AddComponents(params)` to rebuild the array once.
- **The scan cost is linear in component count.** At a handful of components per entity that beats a
  dictionary lookup; at dozens, add an `ICache`.
- **`BasicSpecializedEntity<T>.AllComponents()` returns the live list**, so do not mutate the entity
  while iterating it.

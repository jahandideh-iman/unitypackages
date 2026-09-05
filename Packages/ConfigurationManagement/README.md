# Configuration Management

Applies configuration to objects. You register an `IConfigurer<T>` for a target type with an
`IConfigurationManager`, then hand the manager an instance and it configures it. The Unity layer
backs configurers with `ScriptableObject` assets, so data designers can author values in the Editor.

This is **not** a key/value settings store — there is no `GetConfig("key")`. A configurer receives
the target object and mutates it.

## What it provides

Namespace `Arman.ConfigurationManagement` (core types):

| Type | Purpose |
|---|---|
| `IConfigurer` | `RegisterSelf(IConfigurationManager)` — a configurer adds itself to a manager. |
| `IConfigurer<T>` | Adds `Configure(T entity)`. |
| `IConfigurationManager` | `Register<T>`, `Configure<T>`, `Contains<T>`, `FindConfigurer<T>`, `RemoveConfigurer<T>`. |
| `ConfigurationManager` | The plain C# manager; one configurer per target type. |
| `CompositeConfigurer<T>` | Groups several `IConfigurer<T>` and applies them in order. |
| `DynamicConfigurer<T>` | Built from `Action<T>` delegates added with `AddConfigAction`. |

Namespace `Arman.ConfigurationManagement` (Unity types):

| Type | Purpose |
|---|---|
| `ScriptableConfiguration` | Abstract `ScriptableObject` implementing `IConfigurer`. |
| `UnityConfigurationMaster` | A `ScriptableConfiguration` holding a `ScriptableConfiguration[]`; `RegisterSelf` registers all of them. |
| `UnityConfigurationManager` | `MonoBehaviour` manager that registers its `configurationMaster` on `Init()`. |
| `AutoFillAssetArrayAttribute` | Inspector helper that populates a `ScriptableConfiguration[]` field. |

## Usage

Configuring an object through a dynamic configurer:

```csharp
using Arman.ConfigurationManagement;

var configuration = new ConfigurationManager();

var enemyConfigurer = new DynamicConfigurer<Enemy>();
enemyConfigurer.AddConfigAction(enemy => enemy.Health = 100);
enemyConfigurer.AddConfigAction(enemy => enemy.Speed = 3.5f);

configuration.Register(enemyConfigurer);

// Later, wherever an Enemy is created:
configuration.Configure(newEnemy);
```

Combining several configurers for one type:

```csharp
var composite = new CompositeConfigurer<Enemy>();
composite.AddConfigurer(baseStatsConfigurer);
composite.AddConfigurer(difficultyConfigurer);

composite.RegisterSelf(configuration);
```

Driving it from Editor assets. Write a `ScriptableConfiguration` subclass, register it on a
`UnityConfigurationMaster`, and point a `UnityConfigurationManager` at that master:

```csharp
using Arman.ConfigurationManagement;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Enemy")]
public class EnemyConfiguration : ScriptableConfiguration
{
    [SerializeField] private int _health = 100;

    public override void RegisterSelf(IConfigurationManager manager)
    {
        var configurer = new DynamicConfigurer<Enemy>();
        configurer.AddConfigAction(enemy => enemy.Health = _health);
        manager.Register(configurer);
    }
}
```

```csharp
// unityConfigurationManager is a MonoBehaviour in the scene with configurationMaster assigned.
unityConfigurationManager.Init();
unityConfigurationManager.Configure(newEnemy);
```

## Things to know

- **Namespace simplification.** The runtime namespace is now `Arman.ConfigurationManagement`; the former `Arman.Foundation.Core.ConfigurationManagement` and `Arman.Foundation.Unity.Configuration` namespaces are gone. Update any `using` directives (and test namespaces, now `Arman.ConfigurationManagement.Tests`) to match.
- **`UnityConfigurationManager.Init()` is not implicit.** It is what walks the assigned
  `configurationMaster` and registers every configurer under it.
- **One configurer per target type.** `Register<T>` keys a dictionary on `typeof(T)`, so registering
  a second configurer for the same `T` silently replaces the first. Use `CompositeConfigurer<T>` when
  you want several to apply.
- **`Configure<T>` throws if nothing is registered for `T`.** `FindConfigurer<T>()` returns `null`
  and `Configure` dereferences it — check with `FindConfigurer<T>()` first if the registration is
  optional.
- **`DynamicConfigurer<T>` swallows exceptions per action.** A throwing `Action<T>` is caught and
  logged through `Debug.LogErrorFormat`; the remaining actions still run.
- **`ScriptableConfiguration` is abstract and a `ScriptableObject`** — subclass it and create assets;
  it cannot be constructed with `new`.

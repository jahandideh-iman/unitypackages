# Dependency Resolution

A small reflection-based dependency resolver for wiring an object graph in one place. Register
types, factories or ready-made instances, then build a repository that constructs every
registration exactly once, in dependency order.

## What it provides

Everything lives in the `Arman.DependencyResolution` namespace.

| Type                                | Role                                                                                                                                |
| ----------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `IDependencyResolver`               | The registration surface: `RegisterType<T>()`, `RegisterFactory<T>(Delegate)`, `RegisterInstance<T>(T)`, and `Build()`.             |
| `ReflectionBasedDependencyResolver` | The implementation. Reads constructor and factory parameters through reflection to discover dependencies.                           |
| `IResolutionEntry<T>`               | Returned by every `Register*` call. `As<U>()` also exposes the registration under `U`, and chains.                                  |
| `DependencyResolverExtensions`      | Typed `RegisterFactory` overloads for `Func<TResult>` through `Func<T1, …, T9, TResult>`, so a method group can be passed directly. |
| `IRepository`                       | The result of `Build()`: `Get<T>()` and `TryGet<T>(out T)`.                                                                         |

| Registration                    | Dependencies are                                  | Instance comes from         |
| ------------------------------- | ------------------------------------------------- | --------------------------- |
| `RegisterType<T>()`             | The parameters of `T`'s first public constructor. | That constructor.           |
| `RegisterFactory<T>(factory)`   | The factory's parameters.                         | The factory's return value. |
| `RegisterInstance<T>(instance)` | None.                                             | The instance you passed.    |

## Usage

```csharp
using Arman.DependencyResolution;

IDependencyResolver resolver = new ReflectionBasedDependencyResolver();

resolver.RegisterInstance(settings);
resolver.RegisterType<ScoreService>().As<IScoreService>();
resolver.RegisterFactory<Hud, IScoreService>(Hud.Create);

IRepository repository = resolver.Build();

IScoreService score = repository.Get<IScoreService>();
```

Registration order does not matter — `Build()` sorts the entries so each is constructed after
everything it depends on.

## Things to know

- **Every registration is a singleton within one `Build()`.** Each entry is constructed once, and
  every type it is exposed under (itself plus each `As<U>()`) resolves to that same instance.
- **Construction is eager.** `Build()` creates the whole graph up front; there is no lazy resolution.
  Calling `Build()` again constructs a fresh set of instances (registered instances are, of course,
  reused).
- **Lookup is by exact registered type.** A class registered as `ScoreService` does not satisfy a
  dependency on `IScoreService` unless it was also registered with `.As<IScoreService>()`. The same
  applies to `Get<T>()`.
- **Errors surface at registration where they can, and at `Build()` otherwise:**
  - `As<U>()` throws `ArgumentException` when `T` is not assignable to `U`.
  - `RegisterFactory` throws `ArgumentException` when the factory's return type is not assignable
    to `T`.
  - `RegisterType` throws when `T` has no public constructor.
  - `Build()` throws `InvalidOperationException` for a dependency nothing provides, for a type
    provided by more than one entry, and for a circular dependency (including a type depending on
    itself).
- **Construction exceptions are not wrapped.** An exception thrown by a constructor or factory
  propagates as itself, with its original stack trace, rather than as a `TargetInvocationException`.
- **Registering the same `T` twice replaces the first entry** silently, including any `As<U>()`
  targets it carried.
- **`RegisterType` picks the first public constructor** that reflection reports. Give registered
  types a single public constructor, or use a factory.
- **A missing type in `Get<T>()` throws plain `System.Exception`**; use `TryGet<T>` to probe.
- **Pure C#.** The runtime assembly has no `UnityEngine` dependency and is testable as a plain
  library.

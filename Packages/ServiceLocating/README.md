# Service Locating

A static service locator for registering implementations against an interface and resolving them
from anywhere, without threading dependencies through constructors.

## What it provides

A single static class, `ServiceLocator`, in the `Arman.ServiceLocating` namespace.

| Member | Behaviour |
|---|---|
| `Init()` | Creates the backing instance. Call once at startup — every other member throws `NullReferenceException` until you do. |
| `IsInited()` | Whether `Init()` has run and `Clear()` has not. |
| `Register<TInterface, TImplementation>(impl)` | Registers `impl`, constrained to `TImplementation : TInterface`. |
| `Find<T>()` | Returns the first registration assignable to `T`. Throws if there is none. |
| `UnRegister<T>()` | Removes the registration `Find<T>()` would return. |
| `Replace<TInterface, TImplementation>(impl)` | `UnRegister<TInterface>()` followed by `Register`. |
| `Clear()` | Drops the instance entirely, so `Init()` must be called again. |

## Usage

```csharp
using Arman.ServiceLocating;

ServiceLocator.Init();
ServiceLocator.Register<IScoreService, ScoreService>(new ScoreService());

IScoreService score = ServiceLocator.Find<IScoreService>();
```

Swapping in a test double, and tearing down between tests:

```csharp
ServiceLocator.Replace<IScoreService, FakeScoreService>(new FakeScoreService());

// In teardown:
ServiceLocator.Clear();
```

## Things to know

- **`Init()` is not implicit.** Calling any other member first throws `NullReferenceException`, not a
  helpful message.
- **Resolution is a linear `is T` scan** over the registration list, returning the first match. Fine
  at the handful-of-services scale this is built for; it is not a hot-path lookup.
- **Registration does not detect duplicates.** Registering the same interface twice leaves both in
  the list, and `Find<T>()` returns whichever was added first. Use `Replace` when you mean to
  substitute.
- **A missing service throws plain `System.Exception`**, with the requested type name in the
  message. There is no `TryFind`.
- **The registration list is static and survives scene loads.** Call `Clear()` when you want a fresh
  container, such as between tests.
- **Flat namespace.** The runtime lives in `Arman.ServiceLocating` (formerly
  `Arman.Foundation.Core.ServiceLocating`); the scripts are flat under `Runtime/Scripts`.

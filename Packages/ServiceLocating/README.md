# Service Locating

A lightweight service locator for registering and resolving services by type. Call `RegisterService<T>` once (typically at startup) and retrieve services anywhere with `GetService<T>`, removing the need for constructor injection plumbing.

## What it provides

- `ServiceLocator` — `RegisterService<T>(IService)`, `GetService<T>()`, `Services`; `GetService` asserts the service was registered.
- `IService` — the marker interface for anything that can be located.

## Usage

```csharp
using Arman.ServiceLocating;

var services = new ServiceLocator();
services.RegisterService<IScoreService>(new ScoreService());

IScoreService score = services.GetService<IScoreService>();
```
